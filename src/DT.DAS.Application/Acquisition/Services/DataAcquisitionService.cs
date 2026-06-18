using DT.DAS.Application.Acquisition.Contracts;
using DT.DAS.Application.PostProcessing;
using DT.DAS.Application.PostProcessing.Contracts;
using DT.DAS.Application.Tasks;
using System.Collections.Concurrent;
using System.Data;
using DT.DAS.Application.Acquisition.Utilities;
using DT.DAS.Domain.Entities;
using DT.DAS.Domain.Interfaces;

namespace DT.DAS.Application.Acquisition.Services;

public sealed class DataAcquisitionService : IDataAcquisitionService
{
    private readonly IFileProviderFactory _fileProviderFactory;
    private readonly IDataParserFactory _parserFactory;
    private readonly IDataService _dataService;
    private readonly IAcquisitionLogService _logService;
    private readonly IAcquisitionFileStateService _fileStateService;
    private readonly IPostProcessingService _postProcessingService;
    private readonly SemaphoreSlim _concurrencySemaphore = new(5);
    private readonly ConcurrentDictionary<string, byte> _processingFiles = new(StringComparer.OrdinalIgnoreCase);

    public DataAcquisitionService(
        IFileProviderFactory fileProviderFactory,
        IDataParserFactory parserFactory,
        IDataService dataService,
        IAcquisitionLogService logService,
        IAcquisitionFileStateService fileStateService,
        IPostProcessingService postProcessingService)
    {
        _fileProviderFactory = fileProviderFactory;
        _parserFactory = parserFactory;
        _dataService = dataService;
        _logService = logService;
        _fileStateService = fileStateService;
        _postProcessingService = postProcessingService;
    }

    public async Task<AcquisitionSummary> ExecuteBatchWithTaskLogAsync(IEnumerable<AcquisitionConfig> configs, DateTime start, DateTime end, string taskLogId, CancellationToken ct = default, string? updateSource = null, bool sealOnSuccess = false)
    {
        var configList = configs.ToList();
        var total = configList.Count * ((end.Date - start.Date).Days + 1);
        var summary = new AcquisitionSummary();

        if (total <= 0)
        {
            await _logService.CompleteTaskAsync(taskLogId, "NoData", 0, 0, 0, "No executable acquisition configs.", ct).ConfigureAwait(false);
            return summary;
        }

        updateSource ??= ResolveManualUpdateSource(start, end);
        var tasks = new List<Task>();

        foreach (var config in configList)
        {
            for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
            {
                var targetDate = date;
                tasks.Add(ProcessConfigSafelyAsync(config, targetDate, taskLogId, total, summary, updateSource, ct));
            }
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);

        var finalStatus = summary.SuccessCount > 0 && summary.FailureCount == 0
            ? "Success"
            : summary.SuccessCount == 0 && summary.FailureCount > 0
                ? "Failed"
                : summary.SuccessCount > 0 && summary.FailureCount > 0
                    ? "PartialSuccess"
                    : "NoData";

        await _logService.CompleteTaskAsync(taskLogId, finalStatus, total, summary.SuccessCount, summary.FailureCount, $"Task completed with status {finalStatus}.", ct).ConfigureAwait(false);

        if (sealOnSuccess && finalStatus == "Success")
        {
            await _fileStateService.SealByTaskLogAsync(taskLogId, ct).ConfigureAwait(false);
        }

        return summary;
    }

    private async Task ProcessConfigSafelyAsync(AcquisitionConfig config, DateTime businessDate, string taskLogId, int total, AcquisitionSummary summary, string updateSource, CancellationToken ct)
    {
        await _concurrencySemaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await ProcessSingleConfigAsync(config, businessDate, taskLogId, updateSource, ct).ConfigureAwait(false);
            Interlocked.Increment(ref summary.SuccessCount);
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref summary.FailureCount);
            lock (summary.ErrorDetails)
            {
                summary.ErrorDetails.Add($"[Config:{config.EqName ?? config.Id.ToString()}][Date:{businessDate:yyyy-MM-dd}] {ex.Message}");
            }
        }
        finally
        {
            await _logService.UpdateTaskProgressAsync(
                taskLogId,
                "Running",
                total,
                summary.SuccessCount,
                summary.FailureCount,
                $"Running: {summary.SuccessCount + summary.FailureCount}/{total}",
                ct).ConfigureAwait(false);

            _concurrencySemaphore.Release();
        }
    }

    private async Task ProcessSingleConfigAsync(AcquisitionConfig config, DateTime businessDate, string taskLogId, string updateSource, CancellationToken ct)
    {
        var path = FileDateTimeUtil.GetProcessedFilePath(config, businessDate);
        var fileNamePattern = FileDateTimeUtil.GetProcessedFileName(config, businessDate);
        var provider = _fileProviderFactory.Create(path);
        var targetFiles = new List<string>();

        if (!string.IsNullOrWhiteSpace(fileNamePattern))
        {
            targetFiles.Add(path);
        }
        else
        {
            var pattern = string.IsNullOrWhiteSpace(config.FileType) ? "*.*" : $"*{config.FileType}";
            targetFiles.AddRange(await provider.GetFileNamesAsync(path, pattern, false, ct).ConfigureAwait(false));
        }

        if (targetFiles.Count == 0)
        {
            throw new FileNotFoundException($"No files matched acquisition config path: {path}");
        }

        foreach (var filePath in targetFiles)
        {
            var key = $"{config.Id}:{businessDate:yyyyMMdd}:{filePath}";
            if (!_processingFiles.TryAdd(key, 0))
            {
                continue;
            }

            try
            {
                await ProcessFileAsync(config, businessDate, taskLogId, updateSource, provider, filePath, ct).ConfigureAwait(false);
            }
            finally
            {
                _processingFiles.TryRemove(key, out _);
            }
        }
    }

    private async Task ProcessFileAsync(AcquisitionConfig config, DateTime businessDate, string taskLogId, string updateSource, IFileProvider provider, string filePath, CancellationToken ct)
    {
        var actualFileName = Path.GetFileName(filePath);
        var logEntry = new AcquisitionLogEntry
        {
            TaskLogId = taskLogId,
            ConfigId = config.Id,
            FileName = actualFileName,
            StartTime = DateTime.Now,
            Status = "Running"
        };

        var processedRows = 0;
        try
        {
            if (await _fileStateService.ShouldSkipForSealedAsync(config.Id, businessDate, actualFileName, updateSource, ct).ConfigureAwait(false))
            {
                return;
            }

            if (!provider.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}", filePath);
            }

            var startRow = await _logService.GetNextStartRowAsync(config.Id, actualFileName, ct).ConfigureAwait(false);
            logEntry.StartRow = startRow <= 0 ? config.StartRow : startRow;

            List<Dictionary<string, object?>> rawRows;
            await using (var stream = await provider.GetFileStreamAsync(filePath, ct).ConfigureAwait(false))
            {
                var parser = _parserFactory.Create(filePath);
                var options = CreateParserOptions(config, filePath, logEntry.StartRow);
                rawRows = await parser.ParseAsync<Dictionary<string, object?>>(stream, options, ct).ConfigureAwait(false);
            }

            var processedData = rawRows.Select(row => DataMapperUtil.MapRow(row, config.FieldMappings)).ToList();
            processedRows = processedData.Count;

            if (processedRows > 0)
            {
                if (string.IsNullOrWhiteSpace(config.TableName))
                {
                    throw new InvalidOperationException("Target table name is required.");
                }

                var schema = await _dataService.GetTableSchemaAsync(config.TableName, ct).ConfigureAwait(false);
                var dataToInsert = _dataService.PopulateDataTable(processedData, schema);
                var postRows = ExtractPostProcessingRowKeys(dataToInsert);
                await _dataService.BulkInsertAsync(dataToInsert, config.TableName, ct).ConfigureAwait(false);

                await _postProcessingService.ProcessAsync(new PostProcessingContext
                {
                    Config = config,
                    TaskLogId = taskLogId,
                    BusinessDate = businessDate.Date,
                    SourceTableName = config.TableName,
                    PostTableName = config.PostTableName,
                    FileName = actualFileName,
                    FullPath = filePath,
                    Rows = postRows
                }, ct).ConfigureAwait(false);
            }

            logEntry.ProcessedRows = processedRows;
            logEntry.EndTime = DateTime.Now;
            logEntry.Status = "Success";
            await _logService.RecordLogEntryAsync(logEntry, ct).ConfigureAwait(false);
            await _fileStateService.UpsertSuccessAsync(config, businessDate, filePath, logEntry, updateSource, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logEntry.ProcessedRows = processedRows;
            logEntry.EndTime = DateTime.Now;
            logEntry.Status = "Failed";
            logEntry.ErrorMessage = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
            await _logService.RecordLogEntryAsync(logEntry, ct).ConfigureAwait(false);
            throw;
        }
    }

    private static ParserOptionsBase CreateParserOptions(AcquisitionConfig config, string filePath, int startRow)
    {
        var extension = Path.GetExtension(filePath);
        ParserOptionsBase options = extension.Equals(".csv", StringComparison.OrdinalIgnoreCase)
            ? new CsvParserOptions()
            : new ExcelParserOptions();

        options.HeaderRow = config.HeaderRow <= 0 ? 1 : config.HeaderRow;
        options.StartRow = startRow <= 0 ? config.StartRow : startRow;
        options.FilePath = filePath;
        options.HasExtFields = true;
        return options;
    }

    private static List<PostProcessingRowKey> ExtractPostProcessingRowKeys(DataTable dataTable)
    {
        var result = new List<PostProcessingRowKey>();
        var idColumn = FindColumn(dataTable, "Id");
        if (idColumn == null)
        {
            return result;
        }

        var fullPathColumn = FindColumn(dataTable, "fullFilePath");
        var rowColumn = FindColumn(dataTable, "row");

        foreach (DataRow row in dataTable.Rows)
        {
            if (row.IsNull(idColumn) || !Guid.TryParse(Convert.ToString(row[idColumn]), out var id) || id == Guid.Empty)
            {
                continue;
            }

            result.Add(new PostProcessingRowKey
            {
                Id = id,
                FullPath = fullPathColumn == null || row.IsNull(fullPathColumn) ? null : Convert.ToString(row[fullPathColumn]),
                Row = rowColumn != null && !row.IsNull(rowColumn) && int.TryParse(Convert.ToString(row[rowColumn]), out var rowNumber) ? rowNumber : null
            });
        }

        return result;
    }

    private static DataColumn? FindColumn(DataTable dataTable, string columnName)
    {
        return dataTable.Columns.Cast<DataColumn>().FirstOrDefault(column => string.Equals(column.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));
    }

    private static string ResolveManualUpdateSource(DateTime startDate, DateTime endDate)
    {
        return endDate.Date < DateTime.Today ? FileStateUpdateSources.ManualRepair : FileStateUpdateSources.ManualCurrent;
    }
}




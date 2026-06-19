using DT.DAS.Application.Tasks;
using DT.DAS.Domain.Entities;
using DT.DAS.Domain.Interfaces;

namespace DT.DAS.Application.Tasks.Services;

public sealed class AcquisitionLogService : IAcquisitionLogService
{
    private const int DefaultPageNo = 1;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 200;
    private static readonly HashSet<string> AllowedTriggerTypes = new(StringComparer.OrdinalIgnoreCase) { "MAN", "SCH" };
    private readonly IFileConfigRepository _configRepository;
    private readonly IAcquisitionLogRepository _logRepository;

    public AcquisitionLogService(IFileConfigRepository configRepository, IAcquisitionLogRepository logRepository)
    {
        _configRepository = configRepository;
        _logRepository = logRepository;
    }

    public async Task<int> GetNextStartRowAsync(int configId, string fileName, CancellationToken ct = default)
    {
        if (configId <= 0)
        {
            throw new InvalidOperationException("ConfigId must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new InvalidOperationException("FileName is required.");
        }

        var lastProcessedRow = await _logRepository.GetLastProcessedRowByConfigIdAsync(configId, fileName.Trim(), ct).ConfigureAwait(false);
        if (lastProcessedRow > 0)
        {
            return lastProcessedRow;
        }

        var config = _configRepository.GetListByIds(new[] { configId.ToString() }).FirstOrDefault();
        return config?.StartRow ?? 0;
    }

    public Task<string?> RecordLogEntryAsync(AcquisitionLogEntry entry, CancellationToken ct = default)
    {
        if (entry.ConfigId <= 0)
        {
            throw new InvalidOperationException("ConfigId must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(entry.TaskLogId))
        {
            throw new InvalidOperationException("TaskLogId is required.");
        }

        entry.TaskLogId = entry.TaskLogId.Trim();
        entry.FileName = entry.FileName?.Trim();
        entry.Status = NormalizeStatus(entry.Status);
        return _logRepository.InsertAsync(entry, ct);
    }

    public Task<string?> RecordTaskLogEntryAsync(AcquisitionTaskLogEntry entry, CancellationToken ct = default)
    {
        ValidateTaskLogEntry(entry);
        entry.Status = NormalizeStatus(entry.Status);
        entry.TriggerType = entry.TriggerType?.Trim().ToUpperInvariant();
        entry.TaskCode = entry.TaskCode?.Trim();
        entry.ProcessedCount = entry.SuccessCount + entry.FailureCount;
        entry.Progress = CalculateProgress(entry.TotalConfigs, entry.SuccessCount, entry.FailureCount);
        return _logRepository.InsertAsync(entry, ct);
    }

    public Task<bool> UpdateTaskStatusAsync(string id, string? status, int successCount, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Task.FromResult(false);
        }

        return _logRepository.UpdateAsync(new AcquisitionTaskLogEntry
        {
            Id = id.Trim(),
            Status = NormalizeStatus(status),
            SuccessCount = Math.Max(0, successCount),
            FailureCount = 0,
            ProcessedCount = Math.Max(0, successCount),
            Progress = 100,
            EndTime = DateTime.Now
        }, ct);
    }

    public Task<bool> UpdateTaskProgressAsync(string id, string status, int totalConfigs, int successCount, int failureCount, string? message = null, CancellationToken ct = default)
    {
        return _logRepository.UpdateProgressAsync(new AcquisitionTaskLogEntry
        {
            Id = id,
            Status = NormalizeStatus(status),
            TotalConfigs = Math.Max(0, totalConfigs),
            SuccessCount = Math.Max(0, successCount),
            FailureCount = Math.Max(0, failureCount),
            ProcessedCount = Math.Max(0, successCount) + Math.Max(0, failureCount),
            Progress = CalculateProgress(totalConfigs, successCount, failureCount),
            Message = message
        }, ct);
    }

    public Task<bool> CompleteTaskAsync(string id, string status, int totalConfigs, int successCount, int failureCount, string? message = null, CancellationToken ct = default)
    {
        var normalizedTotal = Math.Max(0, totalConfigs);
        var normalizedSuccess = Math.Max(0, successCount);
        var normalizedFailure = Math.Max(0, failureCount);
        var processedCount = normalizedSuccess + normalizedFailure;
        var progress = normalizedTotal <= 0 || processedCount >= normalizedTotal
            ? 100
            : CalculateProgress(normalizedTotal, normalizedSuccess, normalizedFailure);

        return _logRepository.UpdateAsync(new AcquisitionTaskLogEntry
        {
            Id = id,
            Status = NormalizeStatus(status),
            TotalConfigs = normalizedTotal,
            SuccessCount = normalizedSuccess,
            FailureCount = normalizedFailure,
            ProcessedCount = processedCount,
            Progress = progress,
            Message = message,
            EndTime = DateTime.Now
        }, ct);
    }

    public Task<AcquisitionTaskLogEntry?> GetTaskLogByIdAsync(string taskLogId, CancellationToken ct = default)
    {
        return string.IsNullOrWhiteSpace(taskLogId)
            ? Task.FromResult<AcquisitionTaskLogEntry?>(null)
            : _logRepository.GetTaskLogByIdAsync(taskLogId.Trim(), ct);
    }

    public Task<List<AcquisitionLogEntry>> GetLogsByTaskLogIdAsync(string taskLogId, CancellationToken ct = default)
    {
        return string.IsNullOrWhiteSpace(taskLogId)
            ? Task.FromResult(new List<AcquisitionLogEntry>())
            : _logRepository.GetLogsByTaskLogIdAsync(taskLogId.Trim(), ct);
    }

    public Task<List<AcquisitionTaskLogEntry>> GetTaskLogsAsync(int pageNo, int pageSize, string? status = null, DateTime? startTime = null, DateTime? endTime = null, int? taskId = null, CancellationToken ct = default)
    {
        return _logRepository.GetTaskLogsAsync(NormalizePageNo(pageNo), NormalizePageSize(pageSize), NormalizeStatusFilter(status), startTime, endTime, taskId, ct);
    }

    public Task<int> GetTaskLogsCountAsync(string? status = null, DateTime? startTime = null, DateTime? endTime = null, int? taskId = null, CancellationToken ct = default)
    {
        return _logRepository.GetTaskLogsCountAsync(NormalizeStatusFilter(status), startTime, endTime, taskId, ct);
    }

    public static string NormalizeStatus(string? status)
    {
        return status?.Trim().ToLowerInvariant() switch
        {
            "success" => "Success",
            "failed" or "failure" => "Failed",
            "partialsuccess" or "partial_success" or "partial success" => "PartialSuccess",
            "nodata" or "no_data" or "no data" => "NoData",
            _ => string.IsNullOrWhiteSpace(status) ? "Running" : status.Trim()
        };
    }

    public static int CalculateProgress(int totalConfigs, int successCount, int failureCount)
    {
        if (totalConfigs <= 0)
        {
            return 0;
        }

        var processed = Math.Max(0, successCount) + Math.Max(0, failureCount);
        if (processed <= 0)
        {
            return 0;
        }

        return Math.Clamp((int)Math.Floor(processed * 100.0 / totalConfigs), 0, 100);
    }

    public static int NormalizePageNo(int pageNo) => pageNo <= 0 ? DefaultPageNo : pageNo;

    public static int NormalizePageSize(int pageSize)
    {
        if (pageSize <= 0)
        {
            return DefaultPageSize;
        }

        return Math.Min(pageSize, MaxPageSize);
    }

    private static string? NormalizeStatusFilter(string? status)
    {
        return string.IsNullOrWhiteSpace(status) ? null : NormalizeStatus(status);
    }

    private static void ValidateTaskLogEntry(AcquisitionTaskLogEntry entry)
    {
        if (entry.TaskId < 0)
        {
            throw new InvalidOperationException("TaskId cannot be negative.");
        }

        if (!string.IsNullOrWhiteSpace(entry.TaskCode) && entry.TaskCode.Trim().Length > 50)
        {
            throw new InvalidOperationException("TaskCode length cannot exceed 50 characters.");
        }

        if (!string.IsNullOrWhiteSpace(entry.TriggerType) && !AllowedTriggerTypes.Contains(entry.TriggerType.Trim()))
        {
            throw new InvalidOperationException("TriggerType must be MAN or SCH.");
        }
    }
}

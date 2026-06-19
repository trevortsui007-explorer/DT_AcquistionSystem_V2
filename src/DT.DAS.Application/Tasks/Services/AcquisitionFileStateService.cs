using DT.DAS.Application.Tasks;
using DT.DAS.Domain.Entities;
using DT.DAS.Domain.Interfaces;

namespace DT.DAS.Application.Tasks.Services;

public sealed class AcquisitionFileStateService : IAcquisitionFileStateService
{
    private readonly IAcquisitionFileStateRepository _repository;

    public AcquisitionFileStateService(IAcquisitionFileStateRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> ShouldSkipForSealedAsync(int configId, DateTime businessDate, string fileName, string updateSource, CancellationToken ct = default)
    {
        if (configId <= 0 || string.IsNullOrWhiteSpace(fileName) || IsManualRepair(updateSource))
        {
            return false;
        }

        var state = await _repository.GetAsync(configId, businessDate.Date, fileName.Trim(), ct).ConfigureAwait(false);
        return state?.IsSealed == true;
    }

    public Task<bool> UpsertSuccessAsync(AcquisitionConfig config, DateTime businessDate, string fullPath, AcquisitionLogEntry logEntry, string updateSource, CancellationToken ct = default)
    {
        var baseStartRow = config.StartRow <= 0 ? 1 : config.StartRow;
        var state = new AcquisitionFileState
        {
            ConfigId = config.Id,
            BusinessDate = businessDate.Date,
            FileName = logEntry.FileName ?? Path.GetFileName(fullPath),
            FullPath = fullPath,
            DataRowCount = Math.Max(0, logEntry.StartRow - baseStartRow + logEntry.ProcessedRows),
            LastStartRow = logEntry.StartRow,
            LastProcessedRows = logEntry.ProcessedRows,
            LastTaskLogId = logEntry.TaskLogId,
            LastStatus = "Success",
            LastUpdateSource = NormalizeUpdateSource(updateSource)
        };

        return _repository.UpsertSuccessAsync(state, IsManualRepair(updateSource), ct);
    }

    public Task<int> SealByTaskLogAsync(string taskLogId, CancellationToken ct = default)
    {
        return string.IsNullOrWhiteSpace(taskLogId)
            ? Task.FromResult(0)
            : _repository.SealByTaskLogAsync(taskLogId.Trim(), ct);
    }

    public Task<List<AcquisitionFileState>> GetByConfigAndDateRangeAsync(int configId, DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        if (configId <= 0)
        {
            return Task.FromResult(new List<AcquisitionFileState>());
        }

        var normalizedStart = startDate.Date;
        var normalizedEnd = endDate.Date;
        if (normalizedEnd < normalizedStart)
        {
            throw new InvalidOperationException("结束日期不能早于开始日期");
        }

        return _repository.GetByConfigAndDateRangeAsync(configId, normalizedStart, normalizedEnd, ct);
    }

    private static string NormalizeUpdateSource(string? updateSource)
    {
        return string.IsNullOrWhiteSpace(updateSource)
            ? FileStateUpdateSources.ManualCurrent
            : updateSource.Trim().ToUpperInvariant();
    }

    private static bool IsManualRepair(string? updateSource)
    {
        return string.Equals(NormalizeUpdateSource(updateSource), FileStateUpdateSources.ManualRepair, StringComparison.Ordinal);
    }
}





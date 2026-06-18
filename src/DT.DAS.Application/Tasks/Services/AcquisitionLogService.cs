using DT.DAS.Application.Tasks;
using DT.DAS.Domain.Entities;
using DT.DAS.Domain.Interfaces;

namespace DT.DAS.Application.Tasks.Services;

public sealed class AcquisitionLogService : IAcquisitionLogService
{
    private readonly IFileConfigRepository _configRepository;
    private readonly IAcquisitionLogRepository _logRepository;

    public AcquisitionLogService(IFileConfigRepository configRepository, IAcquisitionLogRepository logRepository)
    {
        _configRepository = configRepository;
        _logRepository = logRepository;
    }

    public async Task<int> GetNextStartRowAsync(int configId, string fileName, CancellationToken ct = default)
    {
        var lastProcessedRow = await _logRepository.GetLastProcessedRowByConfigIdAsync(configId, fileName, ct).ConfigureAwait(false);
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

        entry.Status = NormalizeStatus(entry.Status);
        return _logRepository.InsertAsync(entry, ct);
    }

    public Task<string?> RecordTaskLogEntryAsync(AcquisitionTaskLogEntry entry, CancellationToken ct = default)
    {
        entry.Status = NormalizeStatus(entry.Status);
        entry.ProcessedCount = entry.SuccessCount + entry.FailureCount;
        entry.Progress = CalculateProgress(entry.TotalConfigs, entry.SuccessCount, entry.FailureCount);
        return _logRepository.InsertAsync(entry, ct);
    }

    public Task<bool> UpdateTaskProgressAsync(string id, string status, int totalConfigs, int successCount, int failureCount, string? message = null, CancellationToken ct = default)
    {
        return _logRepository.UpdateProgressAsync(new AcquisitionTaskLogEntry
        {
            Id = id,
            Status = NormalizeStatus(status),
            TotalConfigs = totalConfigs,
            SuccessCount = successCount,
            FailureCount = failureCount,
            ProcessedCount = successCount + failureCount,
            Progress = CalculateProgress(totalConfigs, successCount, failureCount),
            Message = message
        }, ct);
    }

    public Task<bool> CompleteTaskAsync(string id, string status, int totalConfigs, int successCount, int failureCount, string? message = null, CancellationToken ct = default)
    {
        var processedCount = successCount + failureCount;
        var progress = totalConfigs <= 0 || processedCount >= totalConfigs
            ? 100
            : CalculateProgress(totalConfigs, successCount, failureCount);

        return _logRepository.UpdateAsync(new AcquisitionTaskLogEntry
        {
            Id = id,
            Status = NormalizeStatus(status),
            TotalConfigs = totalConfigs,
            SuccessCount = successCount,
            FailureCount = failureCount,
            ProcessedCount = processedCount,
            Progress = progress,
            Message = message,
            EndTime = DateTime.Now
        }, ct);
    }

    public Task<AcquisitionTaskLogEntry?> GetTaskLogByIdAsync(string taskLogId, CancellationToken ct = default)
    {
        return _logRepository.GetTaskLogByIdAsync(taskLogId, ct);
    }

    public Task<List<AcquisitionLogEntry>> GetLogsByTaskLogIdAsync(string taskLogId, CancellationToken ct = default)
    {
        return _logRepository.GetLogsByTaskLogIdAsync(taskLogId, ct);
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

        var processed = successCount + failureCount;
        if (processed <= 0)
        {
            return 0;
        }

        return Math.Clamp((int)Math.Floor(processed * 100.0 / totalConfigs), 0, 100);
    }
}




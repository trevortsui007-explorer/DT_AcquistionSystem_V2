using DT.DAS.Domain.Entities;
using DT.DAS.Domain.Interfaces;

namespace DT.DAS.Tests.TestDoubles;

internal sealed class InMemoryAcquisitionLogRepository : IAcquisitionLogRepository
{
    private readonly List<AcquisitionLogEntry> _detailLogs = new();
    private readonly List<AcquisitionTaskLogEntry> _taskLogs = new();
    private readonly Dictionary<(int ConfigId, string FileName), int> _nextRows = new();
    private int _nextId;

    public int LastPageNo { get; private set; }
    public int LastPageSize { get; private set; }
    public string? LastStatusFilter { get; private set; }
    public DateTime? LastStartTimeFilter { get; private set; }
    public DateTime? LastEndTimeFilter { get; private set; }
    public int? LastTaskIdFilter { get; private set; }
    public AcquisitionLogEntry? LastInsertedLog { get; private set; }
    public AcquisitionTaskLogEntry? LastInsertedTaskLog { get; private set; }
    public AcquisitionTaskLogEntry? LastUpdatedTaskLog { get; private set; }

    public void SetNextStartRow(int configId, string fileName, int row)
    {
        _nextRows[(configId, fileName)] = row;
    }

    public Task<string?> InsertAsync(AcquisitionLogEntry entry, CancellationToken ct = default)
    {
        entry.Id = Interlocked.Increment(ref _nextId).ToString();
        LastInsertedLog = entry;
        _detailLogs.Add(Clone(entry));
        return Task.FromResult<string?>(entry.Id);
    }

    public Task<string?> InsertAsync(AcquisitionTaskLogEntry entry, CancellationToken ct = default)
    {
        entry.Id = Interlocked.Increment(ref _nextId).ToString();
        LastInsertedTaskLog = entry;
        _taskLogs.Add(Clone(entry));
        return Task.FromResult<string?>(entry.Id);
    }

    public Task<bool> UpdateAsync(AcquisitionTaskLogEntry entry, CancellationToken ct = default)
    {
        LastUpdatedTaskLog = entry;
        var existing = _taskLogs.FirstOrDefault(x => x.Id == entry.Id);
        if (existing == null)
        {
            return Task.FromResult(false);
        }

        existing.EndTime = entry.EndTime;
        existing.Status = entry.Status;
        existing.SuccessCount = entry.SuccessCount;
        existing.FailureCount = entry.FailureCount;
        existing.ProcessedCount = entry.ProcessedCount;
        existing.Progress = entry.Progress;
        existing.Message = entry.Message;
        return Task.FromResult(true);
    }

    public Task<bool> UpdateProgressAsync(AcquisitionTaskLogEntry entry, CancellationToken ct = default)
    {
        LastUpdatedTaskLog = entry;
        var existing = _taskLogs.FirstOrDefault(x => x.Id == entry.Id);
        if (existing == null)
        {
            return Task.FromResult(false);
        }

        existing.Status = entry.Status;
        existing.SuccessCount = entry.SuccessCount;
        existing.FailureCount = entry.FailureCount;
        existing.ProcessedCount = entry.ProcessedCount;
        existing.Progress = entry.Progress;
        existing.Message = entry.Message;
        return Task.FromResult(true);
    }

    public Task<AcquisitionTaskLogEntry?> GetTaskLogByIdAsync(string taskLogId, CancellationToken ct = default)
    {
        return Task.FromResult(_taskLogs.FirstOrDefault(x => x.Id == taskLogId));
    }

    public Task<List<AcquisitionLogEntry>> GetLogsByTaskLogIdAsync(string taskLogId, CancellationToken ct = default)
    {
        return Task.FromResult(_detailLogs.Where(x => x.TaskLogId == taskLogId).Select(Clone).ToList());
    }

    public Task<int> GetLastProcessedRowByConfigIdAsync(int configId, string fileName, CancellationToken ct = default)
    {
        return Task.FromResult(_nextRows.TryGetValue((configId, fileName), out var row) ? row : 0);
    }

    public Task<List<AcquisitionTaskLogEntry>> GetTaskLogsAsync(int pageNo, int pageSize, string? status = null, DateTime? startTime = null, DateTime? endTime = null, int? taskId = null, CancellationToken ct = default)
    {
        LastPageNo = pageNo;
        LastPageSize = pageSize;
        LastStatusFilter = status;
        LastStartTimeFilter = startTime;
        LastEndTimeFilter = endTime;
        LastTaskIdFilter = taskId;

        var query = Filter(status, startTime, endTime, taskId);
        return Task.FromResult(query.Skip((pageNo - 1) * pageSize).Take(pageSize).Select(Clone).ToList());
    }

    public Task<int> GetTaskLogsCountAsync(string? status = null, DateTime? startTime = null, DateTime? endTime = null, int? taskId = null, CancellationToken ct = default)
    {
        LastStatusFilter = status;
        LastStartTimeFilter = startTime;
        LastEndTimeFilter = endTime;
        LastTaskIdFilter = taskId;
        return Task.FromResult(Filter(status, startTime, endTime, taskId).Count());
    }

    private IEnumerable<AcquisitionTaskLogEntry> Filter(string? status, DateTime? startTime, DateTime? endTime, int? taskId)
    {
        return _taskLogs
            .Where(x => string.IsNullOrWhiteSpace(status) || x.Status == status)
            .Where(x => taskId == null || x.TaskId == taskId)
            .Where(x => startTime == null || x.StartTime >= startTime)
            .Where(x => endTime == null || x.StartTime <= endTime)
            .OrderByDescending(x => x.StartTime)
            .ThenByDescending(x => x.Id);
    }

    private static AcquisitionTaskLogEntry Clone(AcquisitionTaskLogEntry entry)
    {
        return new AcquisitionTaskLogEntry
        {
            Id = entry.Id,
            TaskId = entry.TaskId,
            TaskCode = entry.TaskCode,
            TriggerType = entry.TriggerType,
            StartTime = entry.StartTime,
            EndTime = entry.EndTime,
            Status = entry.Status,
            TotalConfigs = entry.TotalConfigs,
            SuccessCount = entry.SuccessCount,
            FailureCount = entry.FailureCount,
            ProcessedCount = entry.ProcessedCount,
            Progress = entry.Progress,
            Message = entry.Message
        };
    }

    private static AcquisitionLogEntry Clone(AcquisitionLogEntry entry)
    {
        return new AcquisitionLogEntry
        {
            Id = entry.Id,
            TaskLogId = entry.TaskLogId,
            ConfigId = entry.ConfigId,
            FileName = entry.FileName,
            StartRow = entry.StartRow,
            ProcessedRows = entry.ProcessedRows,
            StartTime = entry.StartTime,
            EndTime = entry.EndTime,
            Status = entry.Status,
            ErrorMessage = entry.ErrorMessage
        };
    }
}

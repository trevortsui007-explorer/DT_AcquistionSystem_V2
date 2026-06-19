using DT.DAS.Domain.Entities;

namespace DT.DAS.Application.Tasks;

public interface IAcquisitionLogService
{
    Task<int> GetNextStartRowAsync(int configId, string fileName, CancellationToken ct = default);
    Task<string?> RecordLogEntryAsync(AcquisitionLogEntry entry, CancellationToken ct = default);
    Task<string?> RecordTaskLogEntryAsync(AcquisitionTaskLogEntry entry, CancellationToken ct = default);
    Task<bool> UpdateTaskStatusAsync(string id, string? status, int successCount, CancellationToken ct = default);
    Task<bool> UpdateTaskProgressAsync(string id, string status, int totalConfigs, int successCount, int failureCount, string? message = null, CancellationToken ct = default);
    Task<bool> CompleteTaskAsync(string id, string status, int totalConfigs, int successCount, int failureCount, string? message = null, CancellationToken ct = default);
    Task<AcquisitionTaskLogEntry?> GetTaskLogByIdAsync(string taskLogId, CancellationToken ct = default);
    Task<List<AcquisitionLogEntry>> GetLogsByTaskLogIdAsync(string taskLogId, CancellationToken ct = default);
    Task<List<AcquisitionTaskLogEntry>> GetTaskLogsAsync(int pageNo, int pageSize, string? status = null, DateTime? startTime = null, DateTime? endTime = null, int? taskId = null, CancellationToken ct = default);
    Task<int> GetTaskLogsCountAsync(string? status = null, DateTime? startTime = null, DateTime? endTime = null, int? taskId = null, CancellationToken ct = default);
}

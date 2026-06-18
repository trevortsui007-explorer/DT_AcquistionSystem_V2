using DT.DAS.Domain.Entities;

namespace DT.DAS.Domain.Interfaces;

public interface IAcquisitionLogRepository
{
    Task<string?> InsertAsync(AcquisitionLogEntry entry, CancellationToken ct = default);
    Task<string?> InsertAsync(AcquisitionTaskLogEntry entry, CancellationToken ct = default);
    Task<bool> UpdateAsync(AcquisitionTaskLogEntry entry, CancellationToken ct = default);
    Task<bool> UpdateProgressAsync(AcquisitionTaskLogEntry entry, CancellationToken ct = default);
    Task<AcquisitionTaskLogEntry?> GetTaskLogByIdAsync(string taskLogId, CancellationToken ct = default);
    Task<List<AcquisitionLogEntry>> GetLogsByTaskLogIdAsync(string taskLogId, CancellationToken ct = default);
    Task<int> GetLastProcessedRowByConfigIdAsync(int configId, string fileName, CancellationToken ct = default);
    Task<List<AcquisitionTaskLogEntry>> GetTaskLogsAsync(int pageNo, int pageSize, string? status = null, DateTime? startTime = null, DateTime? endTime = null, int? taskId = null, CancellationToken ct = default);
    Task<int> GetTaskLogsCountAsync(string? status = null, DateTime? startTime = null, DateTime? endTime = null, int? taskId = null, CancellationToken ct = default);
}


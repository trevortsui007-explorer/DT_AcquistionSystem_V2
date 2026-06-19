using DT.DAS.Domain.Entities;

namespace DT.DAS.Application.Tasks;

public interface IAcquisitionFileStateService
{
    Task<bool> ShouldSkipForSealedAsync(int configId, DateTime businessDate, string fileName, string updateSource, CancellationToken ct = default);
    Task<bool> UpsertSuccessAsync(AcquisitionConfig config, DateTime businessDate, string fullPath, AcquisitionLogEntry logEntry, string updateSource, CancellationToken ct = default);
    Task<int> SealByTaskLogAsync(string taskLogId, CancellationToken ct = default);
    Task<List<AcquisitionFileState>> GetByConfigAndDateRangeAsync(int configId, DateTime startDate, DateTime endDate, CancellationToken ct = default);
}

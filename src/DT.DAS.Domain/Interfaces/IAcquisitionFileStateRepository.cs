using DT.DAS.Domain.Entities;

namespace DT.DAS.Domain.Interfaces;

public interface IAcquisitionFileStateRepository
{
    Task<AcquisitionFileState?> GetAsync(int configId, DateTime businessDate, string fileName, CancellationToken ct = default);
    Task<List<AcquisitionFileState>> GetByConfigAndDateRangeAsync(int configId, DateTime startDate, DateTime endDate, CancellationToken ct = default);
    Task<bool> UpsertSuccessAsync(AcquisitionFileState state, bool allowSealedUpdate, CancellationToken ct = default);
    Task<int> SealByTaskLogAsync(string taskLogId, CancellationToken ct = default);
}

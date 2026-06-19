using DT.DAS.Domain.Entities;
using DT.DAS.Domain.Interfaces;

namespace DT.DAS.Tests.TestDoubles;

internal sealed class InMemoryAcquisitionFileStateRepository : IAcquisitionFileStateRepository
{
    private readonly List<AcquisitionFileState> _states = new();

    public int LastConfigId { get; private set; }
    public DateTime LastStartDate { get; private set; }
    public DateTime LastEndDate { get; private set; }

    public void Add(AcquisitionFileState state)
    {
        _states.Add(state);
    }

    public Task<AcquisitionFileState?> GetAsync(int configId, DateTime businessDate, string fileName, CancellationToken ct = default)
    {
        return Task.FromResult(_states.FirstOrDefault(x => x.ConfigId == configId && x.BusinessDate.Date == businessDate.Date && string.Equals(x.FileName, fileName, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<List<AcquisitionFileState>> GetByConfigAndDateRangeAsync(int configId, DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        LastConfigId = configId;
        LastStartDate = startDate.Date;
        LastEndDate = endDate.Date;
        return Task.FromResult(_states
            .Where(x => x.ConfigId == configId)
            .Where(x => x.BusinessDate.Date >= startDate.Date && x.BusinessDate.Date <= endDate.Date)
            .OrderBy(x => x.BusinessDate)
            .ThenBy(x => x.FileName)
            .ToList());
    }

    public Task<bool> UpsertSuccessAsync(AcquisitionFileState state, bool allowSealedUpdate, CancellationToken ct = default)
    {
        var existing = _states.FirstOrDefault(x => x.ConfigId == state.ConfigId && x.BusinessDate.Date == state.BusinessDate.Date && string.Equals(x.FileName, state.FileName, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            _states.Remove(existing);
        }

        _states.Add(state);
        return Task.FromResult(true);
    }

    public Task<int> SealByTaskLogAsync(string taskLogId, CancellationToken ct = default)
    {
        var count = 0;
        foreach (var state in _states.Where(x => x.LastTaskLogId == taskLogId && !x.IsSealed))
        {
            state.IsSealed = true;
            count++;
        }

        return Task.FromResult(count);
    }
}

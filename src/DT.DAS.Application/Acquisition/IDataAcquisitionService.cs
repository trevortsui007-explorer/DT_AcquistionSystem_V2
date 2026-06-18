using DT.DAS.Application.Acquisition.Contracts;
using DT.DAS.Domain.Entities;

namespace DT.DAS.Application.Acquisition;

public interface IDataAcquisitionService
{
    Task<AcquisitionSummary> ExecuteBatchWithTaskLogAsync(IEnumerable<AcquisitionConfig> configs, DateTime start, DateTime end, string taskLogId, CancellationToken ct = default, string? updateSource = null, bool sealOnSuccess = false);
}


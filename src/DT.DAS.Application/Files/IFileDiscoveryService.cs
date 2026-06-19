using DT.DAS.Application.Files.Contracts;
using DT.DAS.Domain.Entities;

namespace DT.DAS.Application.Files;

public interface IFileDiscoveryService
{
    Task<List<FileDiscoveryDto>> GetDetailedDiscoveryAsync(AcquisitionConfig config, DateTime startDate, DateTime endDate, string? user = null, string? pass = null, CancellationToken ct = default);
    Task<GroupFileDiscoveryDto> GetGroupDiscoveryAsync(int groupId, DateTime date, string? user = null, string? pass = null, CancellationToken ct = default);
}

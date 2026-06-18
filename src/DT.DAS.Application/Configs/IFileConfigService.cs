using DT.DAS.Domain.Entities;

namespace DT.DAS.Application.Configs;

public interface IFileConfigService
{
    IEnumerable<AcquisitionConfig> GetByIds(IEnumerable<string> ids, string? tableName = null, string? databaseName = null);
    IEnumerable<AcquisitionConfig> GetConfigsByGroupIds(IEnumerable<string> groupIds);
    IEnumerable<AcquisitionConfig> GetConfigsByTaskIds(IEnumerable<string> taskIds);
    IEnumerable<AcquisitionConfig> GetFileConfigs(FileConfigQueryOptions? options);
}


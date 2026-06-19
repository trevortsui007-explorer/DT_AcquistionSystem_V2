using DT.DAS.Application.Configs.Contracts;
using DT.DAS.Domain.Entities;

namespace DT.DAS.Application.Configs;

public interface IFileConfigService
{
    PagedResult<AcquisitionConfig> GetFileConfigsPaged(FileConfigQueryOptions? options, int page, int limit);
    IEnumerable<AcquisitionConfig> GetFileConfigs(FileConfigQueryOptions? options);
    IEnumerable<AcquisitionConfig> GetByIds(IEnumerable<string> ids, string? tableName = null, string? databaseName = null);
    IEnumerable<AcquisitionConfig> GetConfigsByGroupIds(IEnumerable<string> groupIds, string? tableName = null, string? linkTableName = null, string? databaseName = null);
    IEnumerable<AcquisitionConfig> GetConfigsByTaskIds(IEnumerable<string> taskIds, string? tableName = null, string? databaseName = null);
    IEnumerable<AcquisitionConfigStatus> GetStatusByIds(IEnumerable<string> ids, string? columnName = null, string? tableName = null, string? databaseName = null);
    int CreateConfig(AcquisitionConfig config, string? tableName = null, string? databaseName = null);
    bool UpdateConfig(AcquisitionConfig config, string? tableName = null, string? databaseName = null);
    bool DeleteConfigs(IEnumerable<string> ids, string? tableName = null, string? databaseName = null);
    bool SetEnabledStatus(IEnumerable<string> ids, bool isEnabled, string? tableName = null, string? databaseName = null);
}

using DT.DAS.Application.Configs.Contracts;
using DT.DAS.Domain.Entities;

namespace DT.DAS.Application.Configs;

public interface IFileConfigGroupService
{
    IEnumerable<AcquisitionGroupDto> GetList(string? tableName = null, string? linkTableName = null, string? databaseName = null);
    IEnumerable<AcquisitionGroup> GetByIds(IEnumerable<string> ids, string? tableName = null, string? databaseName = null);
    IEnumerable<AcquisitionGroup> GetStatusByIds(IEnumerable<string> ids, string? columnName = null, string? tableName = null, string? databaseName = null);
    int CreateConfigGroup(AcquisitionGroup group, string? tableName = null, string? databaseName = null);
    bool UpdateConfig(AcquisitionGroup group, string? tableName = null, string? databaseName = null);
    bool DeleteConfigs(IEnumerable<string> ids, string? tableName = null, string? databaseName = null);
    bool SetEnabledStatus(IEnumerable<string> ids, bool isEnabled, string? tableName = null, string? databaseName = null);
    Task<bool> AddConfigsToGroupAsync(int groupId, IReadOnlyCollection<int> configIds, string? linkTableName = null, string? databaseName = null, CancellationToken ct = default);
    bool RemoveConfigsFromGroup(int groupId, IReadOnlyCollection<int> configIds, string? linkTableName = null, string? databaseName = null);
}

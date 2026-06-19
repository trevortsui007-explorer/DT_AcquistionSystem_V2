using DT.DAS.Domain.Entities;

namespace DT.DAS.Domain.Interfaces;

public interface IFileConfigGroupRepository
{
    IEnumerable<AcquisitionGroupListItem> GetList(string? tableName = null, string? linkTableName = null, string? databaseName = null);
    IEnumerable<AcquisitionGroup> GetListByIds(IEnumerable<string> ids, string? tableName = null, string? databaseName = null);
    IEnumerable<AcquisitionConfigStatus> GetStatusListByIds(IEnumerable<string> ids, string? columnName = null, string? tableName = null, string? databaseName = null);
    int Insert(AcquisitionGroup group, string? tableName = null, string? databaseName = null);
    bool Update(AcquisitionGroup group, string? tableName = null, string? databaseName = null);
    bool Delete(IEnumerable<string> ids, string? tableName = null, string? databaseName = null);
    bool SetEnabled(IEnumerable<string> ids, bool isEnabled, string? tableName = null, string? databaseName = null);
    bool AddConfigsToGroup(int groupId, IReadOnlyCollection<int> configIds, string? linkTableName = null, string? databaseName = null);
    bool RemoveConfigsFromGroup(int groupId, IReadOnlyCollection<int> configIds, string? linkTableName = null, string? databaseName = null);
}

using DT.DAS.Domain.Entities;

namespace DT.DAS.Domain.Interfaces;

public interface IAcquisitionTaskRepository
{
    IEnumerable<AcquisitionTask> GetList(string? tableName = null, string? databaseName = null);
    IEnumerable<AcquisitionTaskListItem> GetListWithGroups(string? tableName = null, string? linkTableName = null, string? groupTableName = null, string? groupConfigLinkTableName = null, string? databaseName = null);
    AcquisitionTask? GetById(string id, string? tableName = null, string? databaseName = null);
    AcquisitionTaskListItem? GetByIdWithGroups(string id, string? tableName = null, string? linkTableName = null, string? groupTableName = null, string? groupConfigLinkTableName = null, string? databaseName = null);
    IEnumerable<AcquisitionTask> GetListByIds(IEnumerable<string> ids, string? tableName = null, string? databaseName = null);
    IEnumerable<AcquisitionTask> GetListByMode(int taskMode, string? tableName = null, string? databaseName = null);
    int Insert(AcquisitionTask task, string? tableName = null, string? databaseName = null);
    bool Update(AcquisitionTask task, string? tableName = null, string? databaseName = null);
    bool Delete(IEnumerable<string> ids, string? tableName = null, string? linkTableName = null, string? databaseName = null);
    bool SetEnabled(IEnumerable<string> ids, bool isEnabled, string? tableName = null, string? databaseName = null);
    bool AddToGroups(int taskId, IReadOnlyCollection<int> groupIds, string? linkTableName = null, string? databaseName = null);
    bool RemoveAllGroups(int taskId, string? linkTableName = null, string? databaseName = null);
    IEnumerable<int> GetGroupIdsByTaskId(int taskId, string? linkTableName = null, string? databaseName = null);
}

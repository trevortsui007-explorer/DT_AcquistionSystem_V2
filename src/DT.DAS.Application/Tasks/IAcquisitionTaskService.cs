using DT.DAS.Application.Tasks.Contracts;
using DT.DAS.Domain.Entities;

namespace DT.DAS.Application.Tasks;

public interface IAcquisitionTaskService
{
    IEnumerable<AcquisitionTaskDto> GetList(string? tableName = null, string? databaseName = null);
    AcquisitionTaskDto? GetById(string id, string? tableName = null, string? databaseName = null);
    IEnumerable<AcquisitionTask> GetByIds(IEnumerable<string> ids, string? tableName = null, string? databaseName = null);
    IEnumerable<AcquisitionTask> GetByMode(int taskMode, string? tableName = null, string? databaseName = null);
    IEnumerable<int> GetAssociatedGroupIds(int taskId, string? linkTableName = null, string? databaseName = null);
    int CreateTask(AcquisitionTask task, string? tableName = null, string? databaseName = null);
    bool UpdateTask(AcquisitionTask task, string? tableName = null, string? databaseName = null);
    bool DeleteTasks(IEnumerable<string> ids, string? tableName = null, string? databaseName = null);
    bool SetEnabledStatus(IEnumerable<string> ids, bool isEnabled, string? tableName = null, string? databaseName = null);
    bool AssignGroupsToTask(int taskId, IReadOnlyCollection<int> groupIds, string? linkTableName = null, string? databaseName = null);
}

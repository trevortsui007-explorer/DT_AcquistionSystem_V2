using DT.DAS.Domain.Entities;
using DT.DAS.Domain.Interfaces;

namespace DT.DAS.Tests.TestDoubles;

internal sealed class RecordingAcquisitionTaskRepository : IAcquisitionTaskRepository
{
    private readonly List<AcquisitionTask> _tasks = new()
    {
        new AcquisitionTask { Id = 1, TaskName = "T-1", TaskMode = 1, IsEnabled = true, CreateTime = DateTime.Today, UpdateTime = DateTime.Today }
    };

    public bool RemoveAllGroupsCalled { get; private set; }
    public IReadOnlyCollection<int> AddedGroupIds { get; private set; } = Array.Empty<int>();
    public AcquisitionTask? InsertedTask { get; private set; }
    public AcquisitionTask? UpdatedTask { get; private set; }

    public IEnumerable<AcquisitionTask> GetList(string? tableName = null, string? databaseName = null) => _tasks;
    public IEnumerable<AcquisitionTaskListItem> GetListWithGroups(string? tableName = null, string? linkTableName = null, string? groupTableName = null, string? groupConfigLinkTableName = null, string? databaseName = null) => new[] { CreateTaskItem() };
    public AcquisitionTask? GetById(string id, string? tableName = null, string? databaseName = null) => _tasks.FirstOrDefault(x => x.Id.ToString() == id);
    public AcquisitionTaskListItem? GetByIdWithGroups(string id, string? tableName = null, string? linkTableName = null, string? groupTableName = null, string? groupConfigLinkTableName = null, string? databaseName = null) => id == "1" ? CreateTaskItem() : null;
    public IEnumerable<AcquisitionTask> GetListByIds(IEnumerable<string> ids, string? tableName = null, string? databaseName = null) => _tasks.Where(x => ids.Contains(x.Id.ToString()));
    public IEnumerable<AcquisitionTask> GetListByMode(int taskMode, string? tableName = null, string? databaseName = null) => _tasks.Where(x => x.TaskMode == taskMode);
    public int Insert(AcquisitionTask task, string? tableName = null, string? databaseName = null) { InsertedTask = task; return 9; }
    public bool Update(AcquisitionTask task, string? tableName = null, string? databaseName = null) { UpdatedTask = task; return task.Id > 0; }
    public bool Delete(IEnumerable<string> ids, string? tableName = null, string? linkTableName = null, string? databaseName = null) => ids.Any();
    public bool SetEnabled(IEnumerable<string> ids, bool isEnabled, string? tableName = null, string? databaseName = null) => ids.Any();
    public bool AddToGroups(int taskId, IReadOnlyCollection<int> groupIds, string? linkTableName = null, string? databaseName = null) { AddedGroupIds = groupIds.ToArray(); return true; }
    public bool RemoveAllGroups(int taskId, string? linkTableName = null, string? databaseName = null) { RemoveAllGroupsCalled = true; return taskId > 0; }
    public IEnumerable<int> GetGroupIdsByTaskId(int taskId, string? linkTableName = null, string? databaseName = null) => new[] { 1, 2 };

    private static AcquisitionTaskListItem CreateTaskItem()
    {
        return new AcquisitionTaskListItem
        {
            Id = 1,
            TaskName = "T-1",
            TaskMode = 1,
            IsEnabled = true,
            GroupCount = 1,
            GroupIds = new[] { 1 },
            AssociatedGroups = new[] { new TaskAssociatedGroup { Id = 1, GroupName = "G-1", IsEnabled = true, ConfigCount = 1 } }
        };
    }
}

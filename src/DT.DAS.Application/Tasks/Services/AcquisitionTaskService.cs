using DT.DAS.Application.Tasks.Contracts;
using DT.DAS.Domain.Entities;
using DT.DAS.Domain.Interfaces;

namespace DT.DAS.Application.Tasks.Services;

public sealed class AcquisitionTaskService : IAcquisitionTaskService
{
    private readonly IAcquisitionTaskRepository _repository;

    public AcquisitionTaskService(IAcquisitionTaskRepository repository)
    {
        _repository = repository;
    }

    public IEnumerable<AcquisitionTaskDto> GetList(string? tableName = null, string? databaseName = null)
    {
        return _repository.GetListWithGroups(tableName, databaseName: databaseName).Select(MapTask);
    }

    public AcquisitionTaskDto? GetById(string id, string? tableName = null, string? databaseName = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var task = _repository.GetByIdWithGroups(id, tableName, databaseName: databaseName);
        return task == null ? null : MapTask(task);
    }

    public IEnumerable<AcquisitionTask> GetByIds(IEnumerable<string> ids, string? tableName = null, string? databaseName = null)
    {
        var idArray = NormalizeIds(ids);
        return idArray.Length == 0 ? Enumerable.Empty<AcquisitionTask>() : _repository.GetListByIds(idArray, tableName, databaseName);
    }

    public IEnumerable<AcquisitionTask> GetByMode(int taskMode, string? tableName = null, string? databaseName = null)
    {
        return _repository.GetListByMode(taskMode, tableName, databaseName);
    }

    public IEnumerable<int> GetAssociatedGroupIds(int taskId, string? linkTableName = null, string? databaseName = null)
    {
        return taskId <= 0 ? Enumerable.Empty<int>() : _repository.GetGroupIdsByTaskId(taskId, linkTableName, databaseName);
    }

    public int CreateTask(AcquisitionTask task, string? tableName = null, string? databaseName = null)
    {
        ArgumentNullException.ThrowIfNull(task);
        var now = DateTime.Now;
        task.CreateTime = now;
        task.UpdateTime = now;
        return _repository.Insert(task, tableName, databaseName);
    }

    public bool UpdateTask(AcquisitionTask task, string? tableName = null, string? databaseName = null)
    {
        if (task.Id <= 0)
        {
            return false;
        }

        task.UpdateTime = DateTime.Now;
        return _repository.Update(task, tableName, databaseName);
    }

    public bool DeleteTasks(IEnumerable<string> ids, string? tableName = null, string? databaseName = null)
    {
        var idArray = NormalizeIds(ids);
        return idArray.Length > 0 && _repository.Delete(idArray, tableName, null, databaseName);
    }

    public bool SetEnabledStatus(IEnumerable<string> ids, bool isEnabled, string? tableName = null, string? databaseName = null)
    {
        var idArray = NormalizeIds(ids);
        return idArray.Length > 0 && _repository.SetEnabled(idArray, isEnabled, tableName, databaseName);
    }

    public bool AssignGroupsToTask(int taskId, IReadOnlyCollection<int> groupIds, string? linkTableName = null, string? databaseName = null)
    {
        if (taskId <= 0)
        {
            return false;
        }

        if (!_repository.RemoveAllGroups(taskId, linkTableName, databaseName))
        {
            return false;
        }

        var distinctGroupIds = groupIds.Where(id => id > 0).Distinct().ToArray();
        return distinctGroupIds.Length == 0 || _repository.AddToGroups(taskId, distinctGroupIds, linkTableName, databaseName);
    }

    private static AcquisitionTaskDto MapTask(AcquisitionTaskListItem task)
    {
        return new AcquisitionTaskDto
        {
            Id = task.Id,
            TaskName = task.TaskName,
            TaskMode = task.TaskMode,
            CronExpression = task.CronExpression,
            IsEnabled = task.IsEnabled,
            Description = task.Description,
            CreateTime = task.CreateTime,
            UpdateTime = task.UpdateTime,
            GroupCount = task.GroupCount,
            GroupIds = task.GroupIds,
            AssociatedGroups = task.AssociatedGroups.Select(group => new TaskAssociatedGroupDto
            {
                Id = group.Id,
                GroupName = group.GroupName,
                GroupCategory = group.GroupCategory,
                GroupType = group.GroupType,
                ConfigCount = group.ConfigCount,
                IsEnabled = group.IsEnabled
            }).ToArray()
        };
    }

    private static string[] NormalizeIds(IEnumerable<string> ids)
    {
        return ids.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }
}

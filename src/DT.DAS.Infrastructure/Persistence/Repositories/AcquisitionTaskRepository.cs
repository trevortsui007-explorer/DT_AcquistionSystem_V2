using Dapper;
using DT.DAS.Domain.Entities;
using DT.DAS.Domain.Interfaces;
using DT.DAS.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace DT.DAS.Infrastructure.Persistence.Repositories;

public sealed class AcquisitionTaskRepository : IAcquisitionTaskRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly DasDatabaseOptions _options;

    public AcquisitionTaskRepository(ISqlConnectionFactory connectionFactory, IOptions<DasDatabaseOptions> options)
    {
        _connectionFactory = connectionFactory;
        _options = options.Value;
    }

    public IEnumerable<AcquisitionTask> GetList(string? tableName = null, string? databaseName = null)
    {
        using var connection = _connectionFactory.Create(databaseName);
        return connection.Query<AcquisitionTask>($"SELECT * FROM {ResolveTaskTable(tableName)} ORDER BY [CreateTime] DESC").ToList();
    }

    public IEnumerable<AcquisitionTaskListItem> GetListWithGroups(string? tableName = null, string? linkTableName = null, string? groupTableName = null, string? groupConfigLinkTableName = null, string? databaseName = null)
    {
        using var connection = _connectionFactory.Create(databaseName);
        var tasks = connection.Query<AcquisitionTaskListItem>($"""
            SELECT [Id], [TaskName], [TaskMode], [CronExpression], [IsEnabled], [Description], [CreateTime], [UpdateTime]
            FROM {ResolveTaskTable(tableName)}
            ORDER BY [CreateTime] DESC
            """).ToList();

        AttachGroups(connection, tasks, linkTableName, groupTableName, groupConfigLinkTableName);
        return tasks;
    }

    public AcquisitionTask? GetById(string id, string? tableName = null, string? databaseName = null)
    {
        using var connection = _connectionFactory.Create(databaseName);
        return connection.QuerySingleOrDefault<AcquisitionTask>($"SELECT * FROM {ResolveTaskTable(tableName)} WHERE [Id] = @Id", new { Id = id });
    }

    public AcquisitionTaskListItem? GetByIdWithGroups(string id, string? tableName = null, string? linkTableName = null, string? groupTableName = null, string? groupConfigLinkTableName = null, string? databaseName = null)
    {
        using var connection = _connectionFactory.Create(databaseName);
        var task = connection.QuerySingleOrDefault<AcquisitionTaskListItem>($"""
            SELECT [Id], [TaskName], [TaskMode], [CronExpression], [IsEnabled], [Description], [CreateTime], [UpdateTime]
            FROM {ResolveTaskTable(tableName)}
            WHERE [Id] = @Id
            """, new { Id = id });

        if (task == null)
        {
            return null;
        }

        AttachGroups(connection, new List<AcquisitionTaskListItem> { task }, linkTableName, groupTableName, groupConfigLinkTableName);
        return task;
    }

    public IEnumerable<AcquisitionTask> GetListByIds(IEnumerable<string> ids, string? tableName = null, string? databaseName = null)
    {
        var idArray = ids.ToArray();
        if (idArray.Length == 0)
        {
            return Enumerable.Empty<AcquisitionTask>();
        }

        using var connection = _connectionFactory.Create(databaseName);
        return connection.Query<AcquisitionTask>($"SELECT * FROM {ResolveTaskTable(tableName)} WHERE [Id] IN @Ids ORDER BY [CreateTime] DESC", new { Ids = idArray }).ToList();
    }

    public IEnumerable<AcquisitionTask> GetListByMode(int taskMode, string? tableName = null, string? databaseName = null)
    {
        using var connection = _connectionFactory.Create(databaseName);
        return connection.Query<AcquisitionTask>($"SELECT * FROM {ResolveTaskTable(tableName)} WHERE [TaskMode] = @TaskMode ORDER BY [CreateTime] DESC", new { TaskMode = taskMode }).ToList();
    }

    public int Insert(AcquisitionTask task, string? tableName = null, string? databaseName = null)
    {
        using var connection = _connectionFactory.Create(databaseName);
        return connection.ExecuteScalar<int>($"""
            INSERT INTO {ResolveTaskTable(tableName)} ([TaskName], [TaskMode], [CronExpression], [IsEnabled], [Description], [CreateTime], [UpdateTime])
            VALUES (@TaskName, @TaskMode, @CronExpression, @IsEnabled, @Description, @CreateTime, @UpdateTime);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """, task);
    }

    public bool Update(AcquisitionTask task, string? tableName = null, string? databaseName = null)
    {
        using var connection = _connectionFactory.Create(databaseName);
        return connection.Execute($"""
            UPDATE {ResolveTaskTable(tableName)}
            SET [TaskName] = @TaskName,
                [TaskMode] = @TaskMode,
                [CronExpression] = @CronExpression,
                [IsEnabled] = @IsEnabled,
                [Description] = @Description,
                [UpdateTime] = @UpdateTime
            WHERE [Id] = @Id
            """, task) > 0;
    }

    public bool Delete(IEnumerable<string> ids, string? tableName = null, string? linkTableName = null, string? databaseName = null)
    {
        var idArray = ids.ToArray();
        if (idArray.Length == 0)
        {
            return false;
        }

        using var connection = _connectionFactory.Create(databaseName);
        connection.Execute($"DELETE FROM {ResolveTaskGroupTable(linkTableName)} WHERE [TaskId] IN @Ids", new { Ids = idArray });
        return connection.Execute($"DELETE FROM {ResolveTaskTable(tableName)} WHERE [Id] IN @Ids", new { Ids = idArray }) > 0;
    }

    public bool SetEnabled(IEnumerable<string> ids, bool isEnabled, string? tableName = null, string? databaseName = null)
    {
        var idArray = ids.ToArray();
        if (idArray.Length == 0)
        {
            return false;
        }

        using var connection = _connectionFactory.Create(databaseName);
        return connection.Execute($"UPDATE {ResolveTaskTable(tableName)} SET [IsEnabled] = @IsEnabled, [UpdateTime] = @UpdateTime WHERE [Id] IN @Ids", new { IsEnabled = isEnabled, UpdateTime = DateTime.Now, Ids = idArray }) > 0;
    }

    public bool AddToGroups(int taskId, IReadOnlyCollection<int> groupIds, string? linkTableName = null, string? databaseName = null)
    {
        if (groupIds.Count == 0)
        {
            return true;
        }

        using var connection = _connectionFactory.Create(databaseName);
        var rows = groupIds.Select(groupId => new { TaskId = taskId, GroupId = groupId }).ToArray();
        return connection.Execute($"""
            IF NOT EXISTS (SELECT 1 FROM {ResolveTaskGroupTable(linkTableName)} WHERE [TaskId] = @TaskId AND [GroupId] = @GroupId)
            BEGIN
                INSERT INTO {ResolveTaskGroupTable(linkTableName)} ([TaskId], [GroupId]) VALUES (@TaskId, @GroupId)
            END
            """, rows) >= 0;
    }

    public bool RemoveAllGroups(int taskId, string? linkTableName = null, string? databaseName = null)
    {
        using var connection = _connectionFactory.Create(databaseName);
        return connection.Execute($"DELETE FROM {ResolveTaskGroupTable(linkTableName)} WHERE [TaskId] = @TaskId", new { TaskId = taskId }) >= 0;
    }

    public IEnumerable<int> GetGroupIdsByTaskId(int taskId, string? linkTableName = null, string? databaseName = null)
    {
        using var connection = _connectionFactory.Create(databaseName);
        return connection.Query<int>($"SELECT [GroupId] FROM {ResolveTaskGroupTable(linkTableName)} WHERE [TaskId] = @TaskId", new { TaskId = taskId }).ToList();
    }


    private void AttachGroups(Microsoft.Data.SqlClient.SqlConnection connection, IReadOnlyCollection<AcquisitionTaskListItem> tasks, string? linkTableName, string? groupTableName, string? groupConfigLinkTableName)
    {
        if (tasks.Count == 0)
        {
            return;
        }

        var taskIds = tasks.Select(x => x.Id).ToArray();
        var groups = connection.Query<TaskAssociatedGroupRecord>($"""
            SELECT tg.[TaskId],
                   g.[Id],
                   g.[GroupName],
                   g.[GroupCategory],
                   g.[GroupType],
                   g.[IsEnabled],
                   COUNT(gc.[ConfigId]) AS [ConfigCount]
            FROM {ResolveTaskGroupTable(linkTableName)} tg
            INNER JOIN {ResolveGroupTable(groupTableName)} g ON tg.[GroupId] = g.[Id]
            LEFT JOIN {ResolveGroupConfigTable(groupConfigLinkTableName)} gc ON gc.[GroupId] = g.[Id]
            WHERE tg.[TaskId] IN @TaskIds
            GROUP BY tg.[TaskId], g.[Id], g.[GroupName], g.[GroupCategory], g.[GroupType], g.[IsEnabled]
            ORDER BY tg.[TaskId], g.[Id]
            """, new { TaskIds = taskIds }).ToLookup(x => x.TaskId);

        foreach (var task in tasks)
        {
            var associated = groups[task.Id]
                .Select(x => new TaskAssociatedGroup
                {
                    Id = x.Id,
                    GroupName = x.GroupName,
                    GroupCategory = x.GroupCategory,
                    GroupType = x.GroupType,
                    ConfigCount = x.ConfigCount,
                    IsEnabled = x.IsEnabled
                })
                .ToArray();

            task.AssociatedGroups = associated;
            task.GroupIds = associated.Select(x => x.Id).ToArray();
            task.GroupCount = associated.Length;
        }
    }

    private string ResolveTaskTable(string? tableName) => SqlIdentifier.Table(tableName, _options.TaskTableName);
    private string ResolveTaskGroupTable(string? tableName) => SqlIdentifier.Table(tableName, _options.TaskGroupTableName);
    private string ResolveGroupTable(string? tableName) => SqlIdentifier.Table(tableName, _options.GroupTableName);
    private string ResolveGroupConfigTable(string? tableName) => SqlIdentifier.Table(tableName, _options.GroupConfigTableName);

    private sealed class TaskAssociatedGroupRecord
    {
        public int TaskId { get; set; }
        public int Id { get; set; }
        public string? GroupName { get; set; }
        public string? GroupCategory { get; set; }
        public string? GroupType { get; set; }
        public int ConfigCount { get; set; }
        public bool IsEnabled { get; set; }
    }
}


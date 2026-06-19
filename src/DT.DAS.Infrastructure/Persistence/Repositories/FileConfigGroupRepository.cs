using Dapper;
using DT.DAS.Domain.Entities;
using DT.DAS.Domain.Interfaces;
using DT.DAS.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace DT.DAS.Infrastructure.Persistence.Repositories;

public sealed class FileConfigGroupRepository : IFileConfigGroupRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly DasDatabaseOptions _options;

    public FileConfigGroupRepository(ISqlConnectionFactory connectionFactory, IOptions<DasDatabaseOptions> options)
    {
        _connectionFactory = connectionFactory;
        _options = options.Value;
    }

    public IEnumerable<AcquisitionGroupListItem> GetList(string? tableName = null, string? linkTableName = null, string? databaseName = null)
    {
        var groupTable = ResolveGroupTable(tableName);
        var groupTableRaw = SqlIdentifier.RawName(tableName, _options.GroupTableName);
        var groupConfigTable = ResolveGroupConfigTable(linkTableName);
        var configTable = SqlIdentifier.Table(null, _options.ConfigTableName);
        var exportProcedureSelect = HasColumn(groupTableRaw, "ExportProcedureName", databaseName)
            ? ", g.[ExportProcedureName]"
            : ", CAST('' AS nvarchar(200)) AS [ExportProcedureName]";

        using var connection = _connectionFactory.Create(databaseName);
        var groups = connection.Query<AcquisitionGroupListItem>($"""
            SELECT g.[Id], g.[GroupName], g.[GroupCategory], g.[GroupType], g.[IsEnabled]{exportProcedureSelect},
                   (SELECT COUNT(1) FROM {groupConfigTable} gc WHERE gc.[GroupId] = g.[Id]) AS [ConfigCount]
            FROM {groupTable} g
            ORDER BY g.[Id] ASC
            """).ToList();

        if (groups.Count == 0)
        {
            return groups;
        }

        var groupIds = groups.Select(x => x.Id).ToArray();
        var configs = connection.Query<GroupConfigFlat>($"""
            SELECT gc.[GroupId], c.[Id], c.[EqName]
            FROM {groupConfigTable} gc
            INNER JOIN {configTable} c ON gc.[ConfigId] = c.[Id]
            WHERE gc.[GroupId] IN @GroupIds
            """, new { GroupIds = groupIds }).ToLookup(x => x.GroupId);

        foreach (var group in groups)
        {
            group.AssociatedConfigs = configs[group.Id]
                .Select(x => new AcquisitionConfigSummary { Id = x.Id, EqName = x.EqName })
                .ToArray();
        }

        return groups;
    }

    public IEnumerable<AcquisitionGroup> GetListByIds(IEnumerable<string> ids, string? tableName = null, string? databaseName = null)
    {
        var idArray = ids.ToArray();
        if (idArray.Length == 0)
        {
            return Enumerable.Empty<AcquisitionGroup>();
        }

        var groupTableRaw = SqlIdentifier.RawName(tableName, _options.GroupTableName);
        var exportProcedureSelect = HasColumn(groupTableRaw, "ExportProcedureName", databaseName)
            ? ", [ExportProcedureName]"
            : ", CAST('' AS nvarchar(200)) AS [ExportProcedureName]";

        using var connection = _connectionFactory.Create(databaseName);
        return connection.Query<AcquisitionGroup>($"""
            SELECT [Id], [GroupName], [GroupCategory], [GroupType], [IsEnabled]{exportProcedureSelect}
            FROM {ResolveGroupTable(tableName)}
            WHERE [Id] IN @Ids
            ORDER BY [Id] ASC
            """, new { Ids = idArray }).ToList();
    }

    public IEnumerable<AcquisitionConfigStatus> GetStatusListByIds(IEnumerable<string> ids, string? columnName = null, string? tableName = null, string? databaseName = null)
    {
        var idArray = ids.ToArray();
        if (idArray.Length == 0)
        {
            return Enumerable.Empty<AcquisitionConfigStatus>();
        }

        using var connection = _connectionFactory.Create(databaseName);
        return connection.Query<AcquisitionConfigStatus>($"""
            SELECT [Id], {SqlIdentifier.Column(columnName, "GroupName")} AS [Name], [IsEnabled]
            FROM {ResolveGroupTable(tableName)}
            WHERE [Id] IN @Ids
            """, new { Ids = idArray }).ToList();
    }

    public int Insert(AcquisitionGroup group, string? tableName = null, string? databaseName = null)
    {
        var groupTableRaw = SqlIdentifier.RawName(tableName, _options.GroupTableName);
        var hasExportProcedureColumn = HasColumn(groupTableRaw, "ExportProcedureName", databaseName);
        var columns = hasExportProcedureColumn
            ? "[GroupName], [GroupCategory], [GroupType], [ExportProcedureName], [IsEnabled]"
            : "[GroupName], [GroupCategory], [GroupType], [IsEnabled]";
        var values = hasExportProcedureColumn
            ? "@GroupName, @GroupCategory, @GroupType, @ExportProcedureName, @IsEnabled"
            : "@GroupName, @GroupCategory, @GroupType, @IsEnabled";

        using var connection = _connectionFactory.Create(databaseName);
        return connection.ExecuteScalar<int>($"""
            INSERT INTO {ResolveGroupTable(tableName)} ({columns})
            VALUES ({values});
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """, group);
    }

    public bool Update(AcquisitionGroup group, string? tableName = null, string? databaseName = null)
    {
        var groupTableRaw = SqlIdentifier.RawName(tableName, _options.GroupTableName);
        var exportProcedureUpdate = HasColumn(groupTableRaw, "ExportProcedureName", databaseName)
            ? "[ExportProcedureName] = @ExportProcedureName,"
            : string.Empty;

        using var connection = _connectionFactory.Create(databaseName);
        return connection.Execute($"""
            UPDATE {ResolveGroupTable(tableName)}
            SET [GroupName] = @GroupName,
                [GroupCategory] = @GroupCategory,
                [GroupType] = @GroupType,
                {exportProcedureUpdate}
                [IsEnabled] = @IsEnabled
            WHERE [Id] = @Id
            """, group) > 0;
    }

    public bool Delete(IEnumerable<string> ids, string? tableName = null, string? databaseName = null)
    {
        using var connection = _connectionFactory.Create(databaseName);
        return connection.Execute($"DELETE FROM {ResolveGroupTable(tableName)} WHERE [Id] IN @Ids", new { Ids = ids.ToArray() }) > 0;
    }

    public bool SetEnabled(IEnumerable<string> ids, bool isEnabled, string? tableName = null, string? databaseName = null)
    {
        using var connection = _connectionFactory.Create(databaseName);
        return connection.Execute($"UPDATE {ResolveGroupTable(tableName)} SET [IsEnabled] = @IsEnabled WHERE [Id] IN @Ids", new { IsEnabled = isEnabled, Ids = ids.ToArray() }) > 0;
    }

    public bool AddConfigsToGroup(int groupId, IReadOnlyCollection<int> configIds, string? linkTableName = null, string? databaseName = null)
    {
        if (configIds.Count == 0)
        {
            return true;
        }

        var rows = configIds.Select(id => new { GroupId = groupId, ConfigId = id }).ToArray();
        using var connection = _connectionFactory.Create(databaseName);
        return connection.Execute($"""
            IF NOT EXISTS (SELECT 1 FROM {ResolveGroupConfigTable(linkTableName)} WHERE [GroupId] = @GroupId AND [ConfigId] = @ConfigId)
            BEGIN
                INSERT INTO {ResolveGroupConfigTable(linkTableName)} ([GroupId], [ConfigId], [IsEnabled])
                VALUES (@GroupId, @ConfigId, 1)
            END
            """, rows) >= 0;
    }

    public bool RemoveConfigsFromGroup(int groupId, IReadOnlyCollection<int> configIds, string? linkTableName = null, string? databaseName = null)
    {
        if (configIds.Count == 0)
        {
            return true;
        }

        using var connection = _connectionFactory.Create(databaseName);
        return connection.Execute($"DELETE FROM {ResolveGroupConfigTable(linkTableName)} WHERE [GroupId] = @GroupId AND [ConfigId] IN @ConfigIds", new { GroupId = groupId, ConfigIds = configIds.ToArray() }) > 0;
    }

    private bool HasColumn(string tableName, string columnName, string? databaseName)
    {
        using var connection = _connectionFactory.Create(databaseName);
        return connection.ExecuteScalar<int?>("SELECT COL_LENGTH(@TableName, @ColumnName)", new { TableName = tableName, ColumnName = columnName }) is not null;
    }

    private string ResolveGroupTable(string? tableName) => SqlIdentifier.Table(tableName, _options.GroupTableName);
    private string ResolveGroupConfigTable(string? tableName) => SqlIdentifier.Table(tableName, _options.GroupConfigTableName);

    private sealed class GroupConfigFlat
    {
        public int GroupId { get; set; }
        public int Id { get; set; }
        public string? EqName { get; set; }
    }
}

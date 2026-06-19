using Dapper;
using DT.DAS.Domain.Entities;
using DT.DAS.Domain.Interfaces;
using DT.DAS.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace DT.DAS.Infrastructure.Persistence.Repositories;

public sealed class FileConfigRepository : IFileConfigRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly DasDatabaseOptions _options;

    public FileConfigRepository(ISqlConnectionFactory connectionFactory, IOptions<DasDatabaseOptions> options)
    {
        _connectionFactory = connectionFactory;
        _options = options.Value;
    }

    public (int Total, IReadOnlyCollection<AcquisitionConfig> List) GetPageList(FileConfigQueryOptions options, int page, int limit, string? tableName = null, string? databaseName = null)
    {
        var table = ResolveTable(tableName ?? options.TableName);
        var parameters = new DynamicParameters();
        var whereSql = " WHERE 1=1 ";
        if (options.Ids is { Count: > 0 })
        {
            whereSql += " AND [Id] IN @Ids ";
            parameters.Add("Ids", options.Ids.ToArray());
        }

        parameters.Add("Skip", (page - 1) * limit);
        parameters.Add("Take", limit);

        using var connection = _connectionFactory.Create(databaseName ?? options.DatabaseName);
        var total = connection.ExecuteScalar<int>($"SELECT COUNT(1) FROM {table} {whereSql}", parameters);
        var list = connection.Query<AcquisitionConfig>($"""
            SELECT * FROM {table}
            {whereSql}
            ORDER BY [Id] DESC
            OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY
            """, parameters).ToList();

        return (total, list);
    }

    public IEnumerable<AcquisitionConfig> GetListByIds(IEnumerable<string> ids, string? tableName = null, string? databaseName = null)
    {
        var idArray = ids.ToArray();
        if (idArray.Length == 0)
        {
            return Enumerable.Empty<AcquisitionConfig>();
        }

        using var connection = _connectionFactory.Create(databaseName);
        return connection.Query<AcquisitionConfig>($"SELECT * FROM {ResolveTable(tableName)} WHERE [Id] IN @Ids", new { Ids = idArray }).ToList();
    }

    public IEnumerable<AcquisitionConfig> GetListByGroupIds(IEnumerable<string> groupIds, string? tableName = null, string? linkTableName = null, string? databaseName = null)
    {
        var ids = groupIds.ToArray();
        if (ids.Length == 0)
        {
            return Enumerable.Empty<AcquisitionConfig>();
        }

        using var connection = _connectionFactory.Create(databaseName);
        var sql = $"""
            SELECT DISTINCT c.*
            FROM {ResolveTable(tableName)} c
            INNER JOIN {ResolveGroupConfigTable(linkTableName)} gc ON c.Id = gc.ConfigId
            WHERE gc.GroupId IN @GroupIds
              AND gc.IsEnabled = 1
              AND c.IsEnabled = 1
            """;
        return connection.Query<AcquisitionConfig>(sql, new { GroupIds = ids }).ToList();
    }

    public IEnumerable<AcquisitionConfig> GetListByTaskIds(IEnumerable<string> taskIds, string? tableName = null, string? databaseName = null)
    {
        var ids = taskIds.ToArray();
        if (ids.Length == 0)
        {
            return Enumerable.Empty<AcquisitionConfig>();
        }

        using var connection = _connectionFactory.Create(databaseName);
        var sql = $"""
            SELECT DISTINCT c.*
            FROM {ResolveTable(tableName)} c
            INNER JOIN {ResolveGroupConfigTable(null)} gc ON c.Id = gc.ConfigId
            INNER JOIN {SqlIdentifier.Table(null, _options.TaskGroupTableName)} tg ON gc.GroupId = tg.GroupId
            INNER JOIN {SqlIdentifier.Table(null, _options.TaskTableName)} t ON tg.TaskId = t.Id
            WHERE t.Id IN @TaskIds
              AND c.IsEnabled = 1
              AND gc.IsEnabled = 1
              AND t.IsEnabled = 1
            """;
        return connection.Query<AcquisitionConfig>(sql, new { TaskIds = ids }).ToList();
    }

    public IEnumerable<AcquisitionConfig> GetList(FileConfigQueryOptions? options = null)
    {
        using var connection = _connectionFactory.Create(options?.DatabaseName);
        return connection.Query<AcquisitionConfig>($"SELECT * FROM {ResolveTable(options?.TableName)}").ToList();
    }

    public IEnumerable<AcquisitionConfigStatus> GetStatusListByIds(IEnumerable<string> ids, string? columnName = null, string? tableName = null, string? databaseName = null)
    {
        var idArray = ids.ToArray();
        if (idArray.Length == 0)
        {
            return Enumerable.Empty<AcquisitionConfigStatus>();
        }

        using var connection = _connectionFactory.Create(databaseName);
        var sql = $"""
            SELECT [Id], {SqlIdentifier.Column(columnName, "EqName")} AS [Name], [IsEnabled]
            FROM {ResolveTable(tableName)}
            WHERE [Id] IN @Ids
            """;
        return connection.Query<AcquisitionConfigStatus>(sql, new { Ids = idArray }).ToList();
    }

    public int Insert(AcquisitionConfig config, string? tableName = null, string? databaseName = null)
    {
        using var connection = _connectionFactory.Create(databaseName);
        var sql = $"""
            INSERT INTO {ResolveTable(tableName)}(
                [EqName], [TableName], [FilePathPattern], [FileNamePattern], [FileType],
                [HeaderRow], [StartRow], [FieldMappings], [ExtFields], [IsEnabled],
                [PostProcessingType], [PostTableName], [ProcedureName], [ServiceName],
                [Flag], [FlagName], [CreateTime]
            )
            VALUES(
                @EqName, @TableName, @FilePathPattern, @FileNamePattern, @FileType,
                @HeaderRow, @StartRow, @FieldMappings, @ExtFields, @IsEnabled,
                @PostProcessingType, @PostTableName, @ProcedureName, @ServiceName,
                @Flag, @FlagName, GETDATE()
            );
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;
        return connection.ExecuteScalar<int>(sql, config);
    }

    public bool Update(AcquisitionConfig config, string? tableName = null, string? databaseName = null)
    {
        using var connection = _connectionFactory.Create(databaseName);
        var sql = $"""
            UPDATE {ResolveTable(tableName)}
            SET [EqName] = @EqName,
                [TableName] = @TableName,
                [FilePathPattern] = @FilePathPattern,
                [FileNamePattern] = @FileNamePattern,
                [FileType] = @FileType,
                [HeaderRow] = @HeaderRow,
                [StartRow] = @StartRow,
                [FieldMappings] = @FieldMappings,
                [ExtFields] = @ExtFields,
                [IsEnabled] = @IsEnabled,
                [PostProcessingType] = @PostProcessingType,
                [PostTableName] = @PostTableName,
                [ProcedureName] = @ProcedureName,
                [ServiceName] = @ServiceName,
                [Flag] = @Flag,
                [FlagName] = @FlagName
            WHERE [Id] = @Id
            """;
        return connection.Execute(sql, config) > 0;
    }

    public bool Delete(IEnumerable<string> ids, string? tableName = null, string? databaseName = null)
    {
        using var connection = _connectionFactory.Create(databaseName);
        return connection.Execute($"DELETE FROM {ResolveTable(tableName)} WHERE [Id] IN @Ids", new { Ids = ids.ToArray() }) > 0;
    }

    public bool SetEnabled(IEnumerable<string> ids, bool isEnabled, string? tableName = null, string? databaseName = null)
    {
        using var connection = _connectionFactory.Create(databaseName);
        return connection.Execute($"UPDATE {ResolveTable(tableName)} SET [IsEnabled] = @IsEnabled WHERE [Id] IN @Ids", new { IsEnabled = isEnabled, Ids = ids.ToArray() }) > 0;
    }

    private string ResolveTable(string? tableName) => SqlIdentifier.Table(tableName, _options.ConfigTableName);
    private string ResolveGroupConfigTable(string? tableName) => SqlIdentifier.Table(tableName, _options.GroupConfigTableName);
}

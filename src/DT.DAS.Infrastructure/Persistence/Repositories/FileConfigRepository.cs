using Dapper;
using DT.DAS.Domain.Entities;
using DT.DAS.Domain.Interfaces;
using DT.DAS.Infrastructure.Options;
using DT.DAS.Infrastructure.Persistence;
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

    public IEnumerable<AcquisitionConfig> GetListByIds(IEnumerable<string> ids, string? tableName = null, string? databaseName = null)
    {
        var idArray = ids.ToArray();
        if (idArray.Length == 0)
        {
            return Enumerable.Empty<AcquisitionConfig>();
        }

        using var connection = _connectionFactory.Create(databaseName);
        return connection.Query<AcquisitionConfig>($"SELECT * FROM [{ResolveTable(tableName)}] WHERE [Id] IN @Ids", new { Ids = idArray }).ToList();
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
            FROM [{ResolveTable(tableName)}] c
            INNER JOIN [{linkTableName ?? _options.GroupConfigTableName}] gc ON c.Id = gc.ConfigId
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
            FROM [{ResolveTable(tableName)}] c
            INNER JOIN [{_options.GroupConfigTableName}] gc ON c.Id = gc.ConfigId
            INNER JOIN [{_options.TaskGroupTableName}] tg ON gc.GroupId = tg.GroupId
            INNER JOIN [{_options.TaskTableName}] t ON tg.TaskId = t.Id
            WHERE t.Id IN @TaskIds
              AND c.IsEnabled = 1
              AND gc.IsEnabled = 1
              AND t.IsEnabled = 1
            """;
        return connection.Query<AcquisitionConfig>(sql, new { TaskIds = ids }).ToList();
    }

    public IEnumerable<AcquisitionConfig> GetList(FileConfigQueryOptions? options = null)
    {
        if (options?.Ids?.Count > 0)
        {
            return GetListByIds(options.Ids, options.TableName, options.DatabaseName);
        }

        if (options?.GroupIds?.Count > 0)
        {
            return GetListByGroupIds(options.GroupIds, options.TableName, null, options.DatabaseName);
        }

        if (options?.TaskIds?.Count > 0)
        {
            return GetListByTaskIds(options.TaskIds, options.TableName, options.DatabaseName);
        }

        using var connection = _connectionFactory.Create(options?.DatabaseName);
        return connection.Query<AcquisitionConfig>($"SELECT * FROM [{ResolveTable(options?.TableName)}]").ToList();
    }

    private string ResolveTable(string? tableName)
    {
        return string.IsNullOrWhiteSpace(tableName) ? _options.ConfigTableName : tableName;
    }
}


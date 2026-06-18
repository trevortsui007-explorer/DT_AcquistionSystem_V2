using Dapper;
using DT.DAS.Domain.Entities;
using DT.DAS.Domain.Interfaces;
using DT.DAS.Infrastructure.Persistence;

namespace DT.DAS.Infrastructure.Persistence.Repositories;

public sealed class AcquisitionLogRepository : IAcquisitionLogRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public AcquisitionLogRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> GetLastProcessedRowByConfigIdAsync(int configId, string fileName, CancellationToken ct = default)
    {
        const string sql = """
            SELECT TOP 1 ([StartRow] + [ProcessedRows]) AS NextStartRow
            FROM [dbo].[DA_AcquisitionLog]
            WHERE [ConfigId] = @ConfigId AND [FileName] = @FileName AND [Status] = 'Success'
            ORDER BY [Id] DESC
            """;
        await using var connection = _connectionFactory.Create();
        return await connection.ExecuteScalarAsync<int?>(new CommandDefinition(sql, new { ConfigId = configId, FileName = fileName }, cancellationToken: ct)).ConfigureAwait(false) ?? 0;
    }

    public async Task<string?> InsertAsync(AcquisitionTaskLogEntry entry, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO [dbo].[DA_AcquisitionTaskLog]
            ([TaskId],[TaskCode],[TriggerType],[StartTime],[EndTime],[Status],[TotalConfigs],[SuccessCount],[FailureCount],[ProcessedCount],[Progress],[Message])
            OUTPUT INSERTED.[Id]
            VALUES (@TaskId,@TaskCode,@TriggerType,@StartTime,@EndTime,@Status,@TotalConfigs,@SuccessCount,@FailureCount,@ProcessedCount,@Progress,@Message);
            """;
        await using var connection = _connectionFactory.Create();
        var id = await connection.ExecuteScalarAsync<string?>(new CommandDefinition(sql, entry, cancellationToken: ct)).ConfigureAwait(false);
        entry.Id = id;
        return id;
    }

    public async Task<string?> InsertAsync(AcquisitionLogEntry entry, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO [dbo].[DA_AcquisitionLog]
            ([TaskLogId],[ConfigId],[FileName],[StartRow],[ProcessedRows],[StartTime],[EndTime],[Status],[ErrorMessage])
            OUTPUT INSERTED.[Id]
            VALUES (@TaskLogId,@ConfigId,@FileName,@StartRow,@ProcessedRows,@StartTime,@EndTime,@Status,@ErrorMessage);
            """;
        await using var connection = _connectionFactory.Create();
        var id = await connection.ExecuteScalarAsync<string?>(new CommandDefinition(sql, entry, cancellationToken: ct)).ConfigureAwait(false);
        entry.Id = id;
        return id;
    }

    public async Task<bool> UpdateAsync(AcquisitionTaskLogEntry entry, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE [dbo].[DA_AcquisitionTaskLog]
            SET [EndTime] = @EndTime,
                [Status] = @Status,
                [SuccessCount] = @SuccessCount,
                [FailureCount] = @FailureCount,
                [ProcessedCount] = @ProcessedCount,
                [Progress] = @Progress,
                [Message] = @Message
            WHERE [Id] = @Id
            """;
        await using var connection = _connectionFactory.Create();
        return await connection.ExecuteAsync(new CommandDefinition(sql, entry, cancellationToken: ct)).ConfigureAwait(false) > 0;
    }

    public async Task<bool> UpdateProgressAsync(AcquisitionTaskLogEntry entry, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE [dbo].[DA_AcquisitionTaskLog]
            SET [Status] = @Status,
                [SuccessCount] = @SuccessCount,
                [FailureCount] = @FailureCount,
                [ProcessedCount] = @ProcessedCount,
                [Progress] = @Progress,
                [Message] = @Message
            WHERE [Id] = @Id
            """;
        await using var connection = _connectionFactory.Create();
        return await connection.ExecuteAsync(new CommandDefinition(sql, entry, cancellationToken: ct)).ConfigureAwait(false) > 0;
    }

    public async Task<AcquisitionTaskLogEntry?> GetTaskLogByIdAsync(string taskLogId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT TOP 1 CAST([Id] AS NVARCHAR(50)) AS [Id],
                   [TaskId],[TaskCode],[TriggerType],[StartTime],[EndTime],[Status],
                   [TotalConfigs],[SuccessCount],[FailureCount],[ProcessedCount],[Progress],[Message]
            FROM [dbo].[DA_AcquisitionTaskLog]
            WHERE [Id] = @TaskLogId
            """;
        await using var connection = _connectionFactory.Create();
        return await connection.QueryFirstOrDefaultAsync<AcquisitionTaskLogEntry>(new CommandDefinition(sql, new { TaskLogId = taskLogId }, cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task<List<AcquisitionLogEntry>> GetLogsByTaskLogIdAsync(string taskLogId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT CAST([Id] AS NVARCHAR(50)) AS [Id],
                   CAST([TaskLogId] AS NVARCHAR(50)) AS [TaskLogId],
                   [ConfigId],[FileName],[StartRow],[ProcessedRows],[StartTime],[EndTime],[Status],[ErrorMessage]
            FROM [dbo].[DA_AcquisitionLog]
            WHERE [TaskLogId] = @TaskLogId
            ORDER BY [StartTime] DESC, [Id] DESC
            """;
        await using var connection = _connectionFactory.Create();
        var rows = await connection.QueryAsync<AcquisitionLogEntry>(new CommandDefinition(sql, new { TaskLogId = taskLogId }, cancellationToken: ct)).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<List<AcquisitionTaskLogEntry>> GetTaskLogsAsync(int pageNo, int pageSize, string? status = null, DateTime? startTime = null, DateTime? endTime = null, int? taskId = null, CancellationToken ct = default)
    {
        const string sql = """
            SELECT CAST([Id] AS NVARCHAR(50)) AS [Id],
                   [TaskId],[TaskCode],[TriggerType],[StartTime],[EndTime],[Status],
                   [TotalConfigs],[SuccessCount],[FailureCount],[ProcessedCount],[Progress],[Message]
            FROM [dbo].[DA_AcquisitionTaskLog]
            WHERE (@Status IS NULL OR [Status] = @Status)
              AND (@TaskId IS NULL OR [TaskId] = @TaskId)
              AND (@StartTime IS NULL OR [StartTime] >= @StartTime)
              AND (@EndTime IS NULL OR [StartTime] <= @EndTime)
            ORDER BY [StartTime] DESC, [Id] DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;
        await using var connection = _connectionFactory.Create();
        var rows = await connection.QueryAsync<AcquisitionTaskLogEntry>(new CommandDefinition(sql, new
        {
            Status = string.IsNullOrWhiteSpace(status) ? null : status.Trim(),
            TaskId = taskId,
            StartTime = startTime,
            EndTime = endTime,
            Offset = (Math.Max(1, pageNo) - 1) * Math.Max(1, pageSize),
            PageSize = Math.Max(1, pageSize)
        }, cancellationToken: ct)).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<int> GetTaskLogsCountAsync(string? status = null, DateTime? startTime = null, DateTime? endTime = null, int? taskId = null, CancellationToken ct = default)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM [dbo].[DA_AcquisitionTaskLog]
            WHERE (@Status IS NULL OR [Status] = @Status)
              AND (@TaskId IS NULL OR [TaskId] = @TaskId)
              AND (@StartTime IS NULL OR [StartTime] >= @StartTime)
              AND (@EndTime IS NULL OR [StartTime] <= @EndTime);
            """;
        await using var connection = _connectionFactory.Create();
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, new
        {
            Status = string.IsNullOrWhiteSpace(status) ? null : status.Trim(),
            TaskId = taskId,
            StartTime = startTime,
            EndTime = endTime
        }, cancellationToken: ct)).ConfigureAwait(false);
    }
}


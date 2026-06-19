using Dapper;
using DT.DAS.Domain.Entities;
using DT.DAS.Domain.Interfaces;
using DT.DAS.Infrastructure.Persistence;

namespace DT.DAS.Infrastructure.Persistence.Repositories;

public sealed class AcquisitionFileStateRepository : IAcquisitionFileStateRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public AcquisitionFileStateRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AcquisitionFileState?> GetAsync(int configId, DateTime businessDate, string fileName, CancellationToken ct = default)
    {
        const string sql = """
            SELECT TOP 1 [Id],[ConfigId],[BusinessDate],[FileName],[FullPath],[DataRowCount],
                   [LastStartRow],[LastProcessedRows],[LastTaskLogId],[LastStatus],[LastUpdateSource],
                   [IsSealed],[SealTime],[LastScanTime],[CreateTime],[UpdateTime]
            FROM [dbo].[DA_AcquisitionFileState]
            WHERE [ConfigId] = @ConfigId AND [BusinessDate] = @BusinessDate AND [FileName] = @FileName
            """;
        await using var connection = _connectionFactory.Create();
        return await connection.QueryFirstOrDefaultAsync<AcquisitionFileState>(new CommandDefinition(sql, new
        {
            ConfigId = configId,
            BusinessDate = businessDate.Date,
            FileName = fileName
        }, cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task<List<AcquisitionFileState>> GetByConfigAndDateRangeAsync(int configId, DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        const string sql = """
            SELECT [Id],[ConfigId],[BusinessDate],[FileName],[FullPath],[DataRowCount],
                   [LastStartRow],[LastProcessedRows],[LastTaskLogId],[LastStatus],[LastUpdateSource],
                   [IsSealed],[SealTime],[LastScanTime],[CreateTime],[UpdateTime]
            FROM [dbo].[DA_AcquisitionFileState]
            WHERE [ConfigId] = @ConfigId
              AND [BusinessDate] >= @StartDate
              AND [BusinessDate] <= @EndDate
            ORDER BY [BusinessDate] ASC, [FileName] ASC, [UpdateTime] DESC
            """;
        await using var connection = _connectionFactory.Create();
        var rows = await connection.QueryAsync<AcquisitionFileState>(new CommandDefinition(sql, new
        {
            ConfigId = configId,
            StartDate = startDate.Date,
            EndDate = endDate.Date
        }, cancellationToken: ct)).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<bool> UpsertSuccessAsync(AcquisitionFileState state, bool allowSealedUpdate, CancellationToken ct = default)
    {
        const string sql = """
            MERGE [dbo].[DA_AcquisitionFileState] WITH (HOLDLOCK) AS target
            USING (SELECT @ConfigId AS [ConfigId], @BusinessDate AS [BusinessDate], @FileName AS [FileName]) AS source
            ON target.[ConfigId] = source.[ConfigId]
               AND target.[BusinessDate] = source.[BusinessDate]
               AND target.[FileName] = source.[FileName]
            WHEN MATCHED AND (target.[IsSealed] = 0 OR @AllowSealedUpdate = 1) THEN
                UPDATE SET [FullPath] = @FullPath,
                           [DataRowCount] = @DataRowCount,
                           [LastStartRow] = @LastStartRow,
                           [LastProcessedRows] = @LastProcessedRows,
                           [LastTaskLogId] = @LastTaskLogId,
                           [LastStatus] = @LastStatus,
                           [LastUpdateSource] = @LastUpdateSource,
                           [LastScanTime] = @Now,
                           [UpdateTime] = @Now
            WHEN NOT MATCHED THEN
                INSERT ([ConfigId],[BusinessDate],[FileName],[FullPath],[DataRowCount],[LastStartRow],
                        [LastProcessedRows],[LastTaskLogId],[LastStatus],[LastUpdateSource],[IsSealed],
                        [LastScanTime],[CreateTime],[UpdateTime])
                VALUES (@ConfigId,@BusinessDate,@FileName,@FullPath,@DataRowCount,@LastStartRow,
                        @LastProcessedRows,@LastTaskLogId,@LastStatus,@LastUpdateSource,0,
                        @Now,@Now,@Now);
            """;
        await using var connection = _connectionFactory.Create();
        return await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            state.ConfigId,
            BusinessDate = state.BusinessDate.Date,
            state.FileName,
            state.FullPath,
            state.DataRowCount,
            state.LastStartRow,
            state.LastProcessedRows,
            state.LastTaskLogId,
            state.LastStatus,
            state.LastUpdateSource,
            AllowSealedUpdate = allowSealedUpdate ? 1 : 0,
            Now = DateTime.Now
        }, cancellationToken: ct)).ConfigureAwait(false) > 0;
    }

    public async Task<int> SealByTaskLogAsync(string taskLogId, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE [dbo].[DA_AcquisitionFileState]
            SET [IsSealed] = 1,
                [SealTime] = CASE WHEN [SealTime] IS NULL THEN @Now ELSE [SealTime] END,
                [UpdateTime] = @Now
            WHERE [LastTaskLogId] = @TaskLogId
              AND [LastStatus] = 'Success'
              AND [IsSealed] = 0
            """;
        await using var connection = _connectionFactory.Create();
        return await connection.ExecuteAsync(new CommandDefinition(sql, new { TaskLogId = taskLogId, Now = DateTime.Now }, cancellationToken: ct)).ConfigureAwait(false);
    }
}



using Dapper;
using DT.DAS.Domain.Entities;
using DT.DAS.Domain.Interfaces;

namespace DT.DAS.Infrastructure.Persistence.Repositories;

public sealed class ReportExportTaskRepository : IReportExportTaskRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public ReportExportTaskRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task EnsureStorageAsync(CancellationToken ct = default)
    {
        const string sql = """
            IF OBJECT_ID('dbo.DA_ReportExportTask', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.DA_ReportExportTask (
                    Id NVARCHAR(64) NOT NULL PRIMARY KEY,
                    GroupIds NVARCHAR(1000) NOT NULL,
                    StartTime DATETIME2 NOT NULL,
                    EndTime DATETIME2 NOT NULL,
                    Status NVARCHAR(32) NOT NULL,
                    Progress INT NOT NULL,
                    Stage NVARCHAR(100) NULL,
                    FilePath NVARCHAR(1000) NULL,
                    FileName NVARCHAR(255) NULL,
                    ErrorMessage NVARCHAR(MAX) NULL,
                    CreatedAt DATETIME2 NOT NULL,
                    ExpiredAt DATETIME2 NOT NULL
                );
            END
            """;
        await using var connection = _connectionFactory.Create();
        await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task CreateAsync(ReportExportTask task, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO dbo.DA_ReportExportTask
            (Id, GroupIds, StartTime, EndTime, Status, Progress, Stage, FilePath, FileName, ErrorMessage, CreatedAt, ExpiredAt)
            VALUES
            (@Id, @GroupIds, @StartTime, @EndTime, @Status, @Progress, @Stage, @FilePath, @FileName, @ErrorMessage, @CreatedAt, @ExpiredAt);
            """;
        await using var connection = _connectionFactory.Create();
        await connection.ExecuteAsync(new CommandDefinition(sql, task, cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task<ReportExportTask?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT Id, GroupIds, StartTime, EndTime, Status, Progress, Stage, FilePath, FileName, ErrorMessage, CreatedAt, ExpiredAt
            FROM dbo.DA_ReportExportTask
            WHERE Id = @Id
            """;
        await using var connection = _connectionFactory.Create();
        return await connection.QueryFirstOrDefaultAsync<ReportExportTask>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task UpdateProgressAsync(string id, string status, int progress, string stage, string? errorMessage = null, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE dbo.DA_ReportExportTask
            SET Status = @Status, Progress = @Progress, Stage = @Stage, ErrorMessage = @ErrorMessage
            WHERE Id = @Id
            """;
        await using var connection = _connectionFactory.Create();
        await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id, Status = status, Progress = progress, Stage = stage, ErrorMessage = errorMessage }, cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task CompleteAsync(string id, string filePath, string fileName, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE dbo.DA_ReportExportTask
            SET Status = @Status, Progress = 100, Stage = @Stage, FilePath = @FilePath, FileName = @FileName, ErrorMessage = NULL
            WHERE Id = @Id
            """;
        await using var connection = _connectionFactory.Create();
        await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id, Status = ReportExportTaskStatus.Success, Stage = "导出完成", FilePath = filePath, FileName = fileName }, cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task FailAsync(string id, string errorMessage, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE dbo.DA_ReportExportTask
            SET Status = @Status, Stage = @Stage, ErrorMessage = @ErrorMessage
            WHERE Id = @Id
            """;
        await using var connection = _connectionFactory.Create();
        await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id, Status = ReportExportTaskStatus.Failed, Stage = "导出失败", ErrorMessage = errorMessage }, cancellationToken: ct)).ConfigureAwait(false);
    }
}

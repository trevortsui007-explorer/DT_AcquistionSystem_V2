using System.Data;
using Dapper;
using DT.DAS.Domain.Interfaces;
using Microsoft.Data.SqlClient;

namespace DT.DAS.Infrastructure.Persistence.Repositories;

public sealed class ReportExportDataProvider : IReportExportDataProvider
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public ReportExportDataProvider(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IList<ReportExportGroupDefinition>> GetGroupDefinitionsAsync(IEnumerable<int> groupIds, CancellationToken ct = default)
    {
        var ids = groupIds.Distinct().OrderBy(x => x).ToArray();
        if (ids.Length == 0)
        {
            return new List<ReportExportGroupDefinition>();
        }

        var hasExportColumn = await HasColumnAsync("dbo.DA_AcquisitionGroup", "ExportProcedureName", ct).ConfigureAwait(false);
        var exportColumn = hasExportColumn ? "[ExportProcedureName]" : "CAST('' AS nvarchar(200)) AS [ExportProcedureName]";
        var sql = $"""
            SELECT [Id] AS [GroupId], [GroupName], {exportColumn}
            FROM dbo.DA_AcquisitionGroup
            WHERE [Id] IN @Ids
            ORDER BY [Id] ASC
            """;
        await using var connection = _connectionFactory.Create();
        var rows = await connection.QueryAsync<ReportExportGroupDefinition>(new CommandDefinition(sql, new { Ids = ids }, cancellationToken: ct)).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<IList<ReportDataSet>> ExecuteGroupReportAsync(int groupId, string procedureName, DateTime startTime, DateTime endTime, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(procedureName))
        {
            throw new InvalidOperationException($"配置组 {groupId} 未配置导出存储过程。");
        }

        var safeProcedureName = SqlIdentifier.RawName(procedureName, procedureName);
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(ct).ConfigureAwait(false);
        await using var command = new SqlCommand(safeProcedureName, connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 0
        };
        command.Parameters.AddWithValue("@StartTime", startTime);
        command.Parameters.AddWithValue("@EndTime", endTime);

        var dataSet = new DataSet();
        using var adapter = new SqlDataAdapter(command);
        await Task.Run(() => adapter.Fill(dataSet), ct).ConfigureAwait(false);

        if (dataSet.Tables.Count == 0)
        {
            throw new InvalidOperationException($"存储过程 {safeProcedureName} 未返回任何结果集。");
        }

        return MapDataSets(dataSet);
    }

    public static IList<ReportDataSet> MapDataSets(DataSet dataSet)
    {
        var result = new List<ReportDataSet>();
        var firstTable = dataSet.Tables[0];
        var hasSheetMeta = firstTable.Columns.Contains("SheetName") && firstTable.Rows.Count == dataSet.Tables.Count - 1;
        if (hasSheetMeta)
        {
            for (var i = 1; i < dataSet.Tables.Count; i++)
            {
                var sheetName = Convert.ToString(firstTable.Rows[i - 1]["SheetName"]);
                result.Add(new ReportDataSet { SheetName = string.IsNullOrWhiteSpace(sheetName) ? $"Sheet{i}" : sheetName!, Data = dataSet.Tables[i] });
            }
            return result;
        }

        for (var i = 0; i < dataSet.Tables.Count; i++)
        {
            result.Add(new ReportDataSet { SheetName = $"Sheet{i + 1}", Data = dataSet.Tables[i] });
        }

        return result;
    }

    private async Task<bool> HasColumnAsync(string tableName, string columnName, CancellationToken ct)
    {
        await using var connection = _connectionFactory.Create();
        return await connection.ExecuteScalarAsync<int?>(new CommandDefinition("SELECT COL_LENGTH(@TableName, @ColumnName)", new { TableName = tableName, ColumnName = columnName }, cancellationToken: ct)).ConfigureAwait(false) is not null;
    }
}

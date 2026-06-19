using System.Data;
using DT.DAS.Domain.Entities;
using DT.DAS.Domain.Interfaces;

namespace DT.DAS.Tests.TestDoubles;

internal sealed class RecordingDataService : IDataService
{
    public string? LastSchemaTableName { get; private set; }
    public string? LastBulkInsertTableName { get; private set; }
    public string? LastProcedureFlag { get; private set; }
    public string? LastProcedureName { get; private set; }
    public string? LastCreateTableName { get; private set; }
    public IReadOnlyCollection<ColumnDefinition> LastCreateColumns { get; private set; } = Array.Empty<ColumnDefinition>();
    public int PopulateCallCount { get; private set; }
    public DataTable Schema { get; } = CreateSchema();

    public Task<DataTable> GetTableSchemaAsync(string tableName, CancellationToken ct = default)
    {
        LastSchemaTableName = tableName;
        return Task.FromResult(Schema);
    }

    public DataTable PopulateDataTable(IEnumerable<IDictionary<string, object?>> data, DataTable schema)
    {
        PopulateCallCount++;
        var table = schema.Clone();
        foreach (var item in data)
        {
            var row = table.NewRow();
            foreach (DataColumn column in table.Columns)
            {
                if (item.TryGetValue(column.ColumnName, out var value))
                {
                    row[column.ColumnName] = value ?? DBNull.Value;
                }
            }

            table.Rows.Add(row);
        }

        return table;
    }

    public Task BulkInsertAsync(DataTable dataTable, string destinationTableName, CancellationToken ct = default)
    {
        LastBulkInsertTableName = destinationTableName;
        return Task.CompletedTask;
    }

    public Task ExecuteStoredProcedureAsync(string? flag, string procedureName, CancellationToken ct = default)
    {
        LastProcedureFlag = flag;
        LastProcedureName = procedureName;
        return Task.CompletedTask;
    }

    public Task CreateTableIfNotExistsAsync(string tableName, IEnumerable<ColumnDefinition> columns, CancellationToken ct = default)
    {
        LastCreateTableName = tableName;
        LastCreateColumns = columns.ToArray();
        return Task.CompletedTask;
    }

    private static DataTable CreateSchema()
    {
        var table = new DataTable();
        var id = new DataColumn("Id", typeof(int))
        {
            AutoIncrement = true,
            AllowDBNull = false
        };
        table.Columns.Add(id);
        table.Columns.Add(new DataColumn("Name", typeof(string)) { AllowDBNull = true, MaxLength = 64 });
        table.PrimaryKey = new[] { id };
        return table;
    }
}

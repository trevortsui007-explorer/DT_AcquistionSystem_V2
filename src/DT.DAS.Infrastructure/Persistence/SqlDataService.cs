using System.Data;
using DT.DAS.Domain.Entities;
using DT.DAS.Domain.Interfaces;
using Microsoft.Data.SqlClient;

namespace DT.DAS.Infrastructure.Persistence;

public sealed class SqlDataService : IDataService
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public SqlDataService(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<DataTable> GetTableSchemaAsync(string tableName, CancellationToken ct = default)
    {
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(ct).ConfigureAwait(false);
        await using var command = new SqlCommand($"SELECT TOP 0 * FROM [{tableName}]", connection);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SchemaOnly | CommandBehavior.KeyInfo, ct).ConfigureAwait(false);
        var table = new DataTable();
        table.Load(reader);
        return table;
    }

    public DataTable PopulateDataTable(IEnumerable<IDictionary<string, object?>> data, DataTable schema)
    {
        var table = schema.Clone();
        foreach (var item in data)
        {
            var row = table.NewRow();
            foreach (DataColumn column in table.Columns)
            {
                if (column.AutoIncrement)
                {
                    continue;
                }

                item.TryGetValue(column.ColumnName, out var value);
                row[column.ColumnName] = ConvertForColumn(value, column);
            }

            table.Rows.Add(row);
        }

        return table;
    }

    public async Task BulkInsertAsync(DataTable dataTable, string destinationTableName, CancellationToken ct = default)
    {
        if (dataTable.Rows.Count == 0)
        {
            return;
        }

        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(ct).ConfigureAwait(false);
        using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, null)
        {
            DestinationTableName = $"[{destinationTableName}]",
            BatchSize = 50000,
            BulkCopyTimeout = 600
        };

        foreach (DataColumn column in dataTable.Columns)
        {
            if (!column.AutoIncrement)
            {
                bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
            }
        }

        await bulkCopy.WriteToServerAsync(dataTable, ct).ConfigureAwait(false);
    }

    public async Task ExecuteStoredProcedureAsync(string? flag, string procedureName, CancellationToken ct = default)
    {
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(ct).ConfigureAwait(false);
        await using var command = new SqlCommand(procedureName, connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 600
        };

        if (!string.IsNullOrWhiteSpace(flag))
        {
            command.Parameters.AddWithValue("@Flag", flag);
        }

        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public async Task CreateTableIfNotExistsAsync(string tableName, IEnumerable<ColumnDefinition> columns, CancellationToken ct = default)
    {
        var columnSql = columns.Select(column =>
        {
            var pk = column.IsPrimaryKey ? "PRIMARY KEY IDENTITY(1,1)" : string.Empty;
            var nullable = column.AllowNull ? "NULL" : "NOT NULL";
            return $"[{column.ColumnName}] {GetSqlType(column.DataType, column.MaxLength)} {pk} {nullable}";
        });

        var sql = $"""
            IF OBJECT_ID('[{tableName}]', 'U') IS NULL
            BEGIN
                CREATE TABLE [{tableName}] (
                    {string.Join(",\n                    ", columnSql)}
                )
            END
            """;

        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(ct).ConfigureAwait(false);
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private static object ConvertForColumn(object? value, DataColumn column)
    {
        if (value == null || value == DBNull.Value || string.IsNullOrWhiteSpace(Convert.ToString(value)))
        {
            return column.AllowDBNull ? DBNull.Value : GetDefaultValue(column.DataType);
        }

        var targetType = Nullable.GetUnderlyingType(column.DataType) ?? column.DataType;
        if (targetType == typeof(Guid))
        {
            return value is Guid guid ? guid : Guid.Parse(Convert.ToString(value)!);
        }

        return targetType == typeof(string) ? Convert.ToString(value)! : Convert.ChangeType(value, targetType);
    }

    private static object GetDefaultValue(Type type)
    {
        if (type == typeof(string)) return string.Empty;
        if (type == typeof(Guid)) return Guid.NewGuid();
        if (type == typeof(DateTime)) return DateTime.UtcNow;
        return type.IsValueType ? Activator.CreateInstance(type)! : DBNull.Value;
    }

    private static string GetSqlType(Type type, int? maxLength)
    {
        var actualType = Nullable.GetUnderlyingType(type) ?? type;
        if (actualType == typeof(int)) return "INT";
        if (actualType == typeof(long)) return "BIGINT";
        if (actualType == typeof(decimal)) return "DECIMAL(18,4)";
        if (actualType == typeof(double)) return "FLOAT";
        if (actualType == typeof(bool)) return "BIT";
        if (actualType == typeof(DateTime)) return "DATETIME2";
        if (actualType == typeof(Guid)) return "UNIQUEIDENTIFIER";
        return maxLength is > 0 and <= 4000 ? $"NVARCHAR({maxLength.Value})" : "NVARCHAR(MAX)";
    }
}



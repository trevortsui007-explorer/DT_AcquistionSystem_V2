using System.Data;
using DT.DAS.Domain.Entities;

namespace DT.DAS.Domain.Interfaces;

public interface IDataService
{
    Task<DataTable> GetTableSchemaAsync(string tableName, CancellationToken ct = default);
    DataTable PopulateDataTable(IEnumerable<IDictionary<string, object?>> data, DataTable schema);
    Task BulkInsertAsync(DataTable dataTable, string destinationTableName, CancellationToken ct = default);
    Task ExecuteStoredProcedureAsync(string? flag, string procedureName, CancellationToken ct = default);
    Task CreateTableIfNotExistsAsync(string tableName, IEnumerable<ColumnDefinition> columns, CancellationToken ct = default);
}


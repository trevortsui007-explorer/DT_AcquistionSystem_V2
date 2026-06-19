using DT.DAS.Application.Data.Contracts;

namespace DT.DAS.Application.Data;

public interface IDataMaintenanceService
{
    Task<BulkImportResult> BulkImportAsync(BulkImportRequest request, CancellationToken ct = default);
    Task ExecutePostProcessAsync(PostProcessRequest request, CancellationToken ct = default);
    Task<object> CreateTableAsync(CreateTableRequest request, CancellationToken ct = default);
    Task<List<TableColumnInfo>> GetTableFieldsAsync(string tableName, CancellationToken ct = default);
}

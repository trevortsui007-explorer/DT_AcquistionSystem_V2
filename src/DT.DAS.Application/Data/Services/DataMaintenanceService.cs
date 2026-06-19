using System.Data;
using DT.DAS.Application.Data.Contracts;
using DT.DAS.Domain.Entities;
using DT.DAS.Domain.Interfaces;

namespace DT.DAS.Application.Data.Services;

public sealed class DataMaintenanceService : IDataMaintenanceService
{
    private readonly IDataService _dataService;

    public DataMaintenanceService(IDataService dataService)
    {
        _dataService = dataService;
    }

    public async Task<BulkImportResult> BulkImportAsync(BulkImportRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.TableName))
        {
            throw new InvalidOperationException("请求参数错误，TableName 不能为空");
        }

        if (request.Data == null)
        {
            throw new InvalidOperationException("待导入数据 Data 不能为空");
        }

        var rows = request.Data.ToArray();
        var schema = await _dataService.GetTableSchemaAsync(request.TableName, ct).ConfigureAwait(false);
        var dataToInsert = _dataService.PopulateDataTable(rows, schema);
        await _dataService.BulkInsertAsync(dataToInsert, request.TableName, ct).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(request.PostProcessSproc))
        {
            await _dataService.ExecuteStoredProcedureAsync(request.Flag, request.PostProcessSproc, ct).ConfigureAwait(false);
        }

        return new BulkImportResult
        {
            RowCount = dataToInsert.Rows.Count,
            TableName = request.TableName
        };
    }

    public Task ExecutePostProcessAsync(PostProcessRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Sproc))
        {
            throw new InvalidOperationException("缺少参数 sproc");
        }

        return _dataService.ExecuteStoredProcedureAsync(request.Flag, request.Sproc, ct);
    }

    public async Task<object> CreateTableAsync(CreateTableRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.TableName))
        {
            throw new InvalidOperationException("表名不能为空");
        }

        if (request.Columns == null || request.Columns.Count == 0)
        {
            throw new InvalidOperationException("列定义不能为空");
        }

        var columns = request.Columns.Select(MapColumn).ToList();
        await _dataService.CreateTableIfNotExistsAsync(request.TableName, columns, ct).ConfigureAwait(false);

        return new
        {
            tableName = request.TableName,
            columnCount = columns.Count
        };
    }

    public async Task<List<TableColumnInfo>> GetTableFieldsAsync(string tableName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            throw new InvalidOperationException("表名不能为空");
        }

        var schema = await _dataService.GetTableSchemaAsync(tableName, ct).ConfigureAwait(false);
        if (schema == null)
        {
            throw new InvalidOperationException($"未找到表: {tableName}");
        }

        return schema.Columns.Cast<DataColumn>().Select(column => new TableColumnInfo
        {
            ColumnName = column.ColumnName,
            DataType = column.DataType.Name,
            AllowDBNull = column.AllowDBNull,
            IsIdentity = column.AutoIncrement,
            IsPrimaryKey = schema.PrimaryKey.Any(pk => pk.ColumnName == column.ColumnName),
            MaxLength = column.MaxLength,
            DefaultValue = column.DefaultValue?.ToString()
        }).ToList();
    }

    public static ColumnDefinition MapColumn(ApiColumnDefinition column)
    {
        if (string.IsNullOrWhiteSpace(column.ColumnName))
        {
            throw new InvalidOperationException("列名不能为空");
        }

        return new ColumnDefinition
        {
            ColumnName = column.ColumnName.Trim(),
            DataType = MapStringToType(column.DataType),
            IsPrimaryKey = column.IsPrimaryKey,
            AllowNull = column.AllowNull,
            MaxLength = column.MaxLength
        };
    }

    public static Type MapStringToType(string? typeName)
    {
        return typeName?.Trim().ToLowerInvariant() switch
        {
            "int" or "integer" => typeof(int),
            "long" or "bigint" => typeof(long),
            "decimal" or "double" or "float" or "number" => typeof(decimal),
            "datetime" or "date" => typeof(DateTime),
            "bool" or "boolean" or "bit" => typeof(bool),
            "guid" or "uuid" => typeof(Guid),
            _ => typeof(string)
        };
    }
}

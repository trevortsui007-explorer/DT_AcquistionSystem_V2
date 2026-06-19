using DT.DAS.Application.Data.Contracts;
using DT.DAS.Application.Data.Services;
using DT.DAS.Infrastructure.Persistence;
using DT.DAS.Tests.TestDoubles;

namespace DT.DAS.Tests.Data;

public sealed class DataMaintenanceServiceTests
{
    [Fact]
    public async Task BulkImport_validates_request_and_calls_data_service()
    {
        var dataService = new RecordingDataService();
        var service = new DataMaintenanceService(dataService);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.BulkImportAsync(new BulkImportRequest { TableName = " " }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.BulkImportAsync(new BulkImportRequest { TableName = "dbo.Target" }));

        var result = await service.BulkImportAsync(new BulkImportRequest
        {
            TableName = "dbo.Target",
            Data = new[] { new Dictionary<string, object?> { ["Name"] = "alpha" } },
            Flag = "F1",
            PostProcessSproc = "dbo.AfterImport"
        });

        Assert.Equal(1, result.RowCount);
        Assert.Equal("dbo.Target", dataService.LastSchemaTableName);
        Assert.Equal("dbo.Target", dataService.LastBulkInsertTableName);
        Assert.Equal("F1", dataService.LastProcedureFlag);
        Assert.Equal("dbo.AfterImport", dataService.LastProcedureName);
        Assert.Equal(1, dataService.PopulateCallCount);
    }

    [Fact]
    public async Task ExecutePostProcess_requires_procedure_name()
    {
        var service = new DataMaintenanceService(new RecordingDataService());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecutePostProcessAsync(new PostProcessRequest()));
    }

    [Fact]
    public async Task CreateTable_maps_api_column_types()
    {
        var dataService = new RecordingDataService();
        var service = new DataMaintenanceService(dataService);

        await service.CreateTableAsync(new CreateTableRequest
        {
            TableName = "dbo.Target",
            Columns = new List<ApiColumnDefinition>
            {
                new() { ColumnName = "Id", DataType = "int", IsPrimaryKey = true, AllowNull = false },
                new() { ColumnName = "CreatedAt", DataType = "datetime" },
                new() { ColumnName = "RowId", DataType = "guid" }
            }
        });

        Assert.Equal("dbo.Target", dataService.LastCreateTableName);
        Assert.Equal(typeof(int), dataService.LastCreateColumns.ElementAt(0).DataType);
        Assert.Equal(typeof(DateTime), dataService.LastCreateColumns.ElementAt(1).DataType);
        Assert.Equal(typeof(Guid), dataService.LastCreateColumns.ElementAt(2).DataType);
    }

    [Fact]
    public async Task GetTableFields_maps_schema_to_dto()
    {
        var service = new DataMaintenanceService(new RecordingDataService());

        var fields = await service.GetTableFieldsAsync("dbo.Target");

        Assert.Equal("Id", fields[0].ColumnName);
        Assert.True(fields[0].IsIdentity);
        Assert.True(fields[0].IsPrimaryKey);
        Assert.Equal("Name", fields[1].ColumnName);
        Assert.Equal(64, fields[1].MaxLength);
    }

    [Fact]
    public void SqlIdentifier_rejects_unsafe_data_identifiers()
    {
        Assert.Equal("[dbo].[Target]", SqlIdentifier.Table("dbo.Target", "fallback"));
        Assert.Equal("[Column_1]", SqlIdentifier.Column("Column_1", "fallback"));
        Assert.Equal("dbo.AfterImport", SqlIdentifier.RawName("dbo.AfterImport", "fallback"));
        Assert.Throws<ArgumentException>(() => SqlIdentifier.Table("dbo.Target;DROP", "fallback"));
        Assert.Throws<ArgumentException>(() => SqlIdentifier.Column("Bad Column", "fallback"));
        Assert.Throws<ArgumentException>(() => SqlIdentifier.RawName("dbo.AfterImport--", "fallback"));
    }
}

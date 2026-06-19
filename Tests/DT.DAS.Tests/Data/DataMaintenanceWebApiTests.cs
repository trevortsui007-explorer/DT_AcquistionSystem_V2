using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using DT.DAS.Tests.TestDoubles;

namespace DT.DAS.Tests.Data;

public sealed class DataMaintenanceWebApiTests
{
    [Fact]
    public void WebApi_discovers_data_routes_with_data_acquisition_prefix()
    {
        using var factory = TestWebApplicationFactory.Create();
        using var scope = factory.Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IActionDescriptorCollectionProvider>();

        var routes = provider.ActionDescriptors.Items
            .Select(x => x.AttributeRouteInfo?.Template)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("api/data-acquisition/bulk-import", routes);
        Assert.Contains("api/data-acquisition/execute-post-process", routes);
        Assert.Contains("api/data-acquisition/create-table", routes);
        Assert.Contains("api/data-acquisition/fields/{tableName}", routes);
    }

    [Fact]
    public async Task Data_endpoints_return_legacy_wrapped_payloads()
    {
        await using var factory = TestWebApplicationFactory.Create();
        using var client = factory.CreateClient();

        var bulk = await client.PostAsync("/api/data-acquisition/bulk-import", Json("""
            { "tableName": "dbo.Target", "data": [ { "Name": "alpha" } ], "flag": "F1", "postProcessSproc": "dbo.AfterImport" }
            """));
        var postProcess = await client.PostAsync("/api/data-acquisition/execute-post-process?flag=F1&sproc=dbo.AfterImport", Json("{}"));
        var createTable = await client.PostAsync("/api/data-acquisition/create-table", Json("""
            { "tableName": "dbo.Target", "columns": [ { "columnName": "Id", "dataType": "int", "isPrimaryKey": true, "allowNull": false } ] }
            """));
        var fields = await client.GetAsync("/api/data-acquisition/fields/dbo.Target");

        Assert.Equal(HttpStatusCode.OK, bulk.StatusCode);
        Assert.Equal(HttpStatusCode.OK, postProcess.StatusCode);
        Assert.Equal(HttpStatusCode.OK, createTable.StatusCode);
        Assert.Equal(HttpStatusCode.OK, fields.StatusCode);
        Assert.Contains("\"code\":1", await bulk.Content.ReadAsStringAsync());
        Assert.Contains("批量导入成功", await bulk.Content.ReadAsStringAsync());
        Assert.Contains("存储过程执行成功", await postProcess.Content.ReadAsStringAsync());
        Assert.Contains("验证/创建成功", await createTable.Content.ReadAsStringAsync());
        Assert.Contains("\"columnName\"", await fields.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Swagger_json_contains_data_routes()
    {
        await using var factory = TestWebApplicationFactory.Create(builder => builder.UseEnvironment("Development"));
        using var client = factory.CreateClient();

        var body = await client.GetStringAsync("/swagger/v1/swagger.json");

        Assert.Contains("/api/data-acquisition/bulk-import", body);
        Assert.Contains("/api/data-acquisition/create-table", body);
        Assert.Contains("/api/data-acquisition/fields/{tableName}", body);
    }

    private static StringContent Json(string value) => new(value, Encoding.UTF8, "application/json");
}

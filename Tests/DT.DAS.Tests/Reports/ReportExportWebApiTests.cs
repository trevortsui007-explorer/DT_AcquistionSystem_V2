using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using DT.DAS.Tests.TestDoubles;

namespace DT.DAS.Tests.Reports;

public sealed class ReportExportWebApiTests
{
    [Fact]
    public void WebApi_discovers_report_export_routes()
    {
        using var factory = TestWebApplicationFactory.Create();
        using var scope = factory.Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IActionDescriptorCollectionProvider>();

        var routes = provider.ActionDescriptors.Items
            .Select(x => x.AttributeRouteInfo?.Template)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("api/data-acquisition/export/tasks", routes);
        Assert.Contains("api/data-acquisition/export/tasks/{id}", routes);
        Assert.Contains("api/data-acquisition/export/tasks/{id}/download", routes);
    }

    [Fact]
    public async Task Report_export_endpoints_return_legacy_wrapped_payloads()
    {
        await using var factory = TestWebApplicationFactory.Create();
        using var client = factory.CreateClient();

        var create = await client.PostAsync("/api/data-acquisition/export/tasks", Json("""
            { "groupIds": [1], "startTime": "2026-06-01T00:00:00", "endTime": "2026-06-01T23:59:59" }
            """));
        var createBody = await create.Content.ReadAsStringAsync();
        var missing = await client.GetAsync("/api/data-acquisition/export/tasks/missing");
        var downloadMissing = await client.GetAsync("/api/data-acquisition/export/tasks/missing/download");

        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        Assert.Equal(HttpStatusCode.OK, missing.StatusCode);
        Assert.Equal(HttpStatusCode.OK, downloadMissing.StatusCode);
        Assert.Contains("\"code\":1", createBody);
        Assert.Contains("报表导出任务已创建", createBody);
        Assert.Contains("导出任务不存在", await missing.Content.ReadAsStringAsync());
        Assert.Contains("导出任务不存在", await downloadMissing.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Swagger_json_contains_report_export_routes()
    {
        await using var factory = TestWebApplicationFactory.Create(builder => builder.UseEnvironment("Development"));
        using var client = factory.CreateClient();

        var body = await client.GetStringAsync("/swagger/v1/swagger.json");

        Assert.Contains("/api/data-acquisition/export/tasks", body);
        Assert.Contains("/api/data-acquisition/export/tasks/{id}/download", body);
    }

    private static StringContent Json(string value) => new(value, Encoding.UTF8, "application/json");
}

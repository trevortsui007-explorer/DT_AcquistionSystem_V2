using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using DT.DAS.Tests.TestDoubles;

namespace DT.DAS.Tests.Logs;

public sealed class AcquisitionLogWebApiTests
{
    [Fact]
    public void WebApi_discovers_log_routes_with_data_acquisition_prefix()
    {
        using var factory = TestWebApplicationFactory.Create();
        using var scope = factory.Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IActionDescriptorCollectionProvider>();

        var routes = provider.ActionDescriptors.Items
            .Select(x => x.AttributeRouteInfo?.Template)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("api/data-acquisition/next-row/{configId:int}", routes);
        Assert.Contains("api/data-acquisition/log", routes);
        Assert.Contains("api/data-acquisition/task-log", routes);
        Assert.Contains("api/data-acquisition/task-log/{id}", routes);
        Assert.Contains("api/data-acquisition/task-log/{id}/logs", routes);
        Assert.Contains("api/data-acquisition/task-logs", routes);
    }

    [Fact]
    public async Task Log_endpoints_return_legacy_wrapped_payloads()
    {
        await using var factory = TestWebApplicationFactory.Create();
        using var client = factory.CreateClient();

        var taskLogJson = """
            { "taskId": 1, "taskCode": "TASK-1", "triggerType": "MAN", "status": "running", "totalConfigs": 2, "successCount": 0, "failureCount": 0 }
            """;
        var taskLog = await client.PostAsync("/api/data-acquisition/task-log", Json(taskLogJson));
        var update = await client.PutAsync("/api/data-acquisition/task-log/1", Json("{ \"status\": \"success\", \"successCount\": 1 }"));
        var detailLog = await client.PostAsync("/api/data-acquisition/log", Json("{ \"taskLogId\": \"1\", \"configId\": 1, \"fileName\": \"input.csv\", \"status\": \"success\" }"));
        var nextRow = await client.GetAsync("/api/data-acquisition/next-row/1?fileName=input.csv");
        var taskLogDetail = await client.GetAsync("/api/data-acquisition/task-log/1");
        var detailLogs = await client.GetAsync("/api/data-acquisition/task-log/1/logs");
        var page = await client.GetAsync("/api/data-acquisition/task-logs?pageNo=0&pageSize=999&status=success");

        Assert.Equal(HttpStatusCode.OK, taskLog.StatusCode);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(HttpStatusCode.OK, detailLog.StatusCode);
        Assert.Equal(HttpStatusCode.OK, nextRow.StatusCode);
        Assert.Equal(HttpStatusCode.OK, taskLogDetail.StatusCode);
        Assert.Equal(HttpStatusCode.OK, detailLogs.StatusCode);
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Contains("\"code\":1", await taskLog.Content.ReadAsStringAsync());
        Assert.Contains("任务状态更新成功", await update.Content.ReadAsStringAsync());
        Assert.Contains("\"nextStartRow\"", await nextRow.Content.ReadAsStringAsync());
        Assert.Contains("\"data\"", await detailLogs.Content.ReadAsStringAsync());
        Assert.Contains("\"count\"", await page.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Swagger_json_contains_log_routes()
    {
        await using var factory = TestWebApplicationFactory.Create(builder => builder.UseEnvironment("Development"));
        using var client = factory.CreateClient();

        var body = await client.GetStringAsync("/swagger/v1/swagger.json");

        Assert.Contains("/api/data-acquisition/next-row/{configId}", body);
        Assert.Contains("/api/data-acquisition/task-log/{id}/logs", body);
        Assert.Contains("/api/data-acquisition/task-logs", body);
        Assert.DoesNotContain("/api/file-configs", body);
        Assert.DoesNotContain("/api/files", body);
    }

    private static StringContent Json(string value) => new(value, Encoding.UTF8, "application/json");
}

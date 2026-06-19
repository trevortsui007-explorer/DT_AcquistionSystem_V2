using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using DT.DAS.Tests.TestDoubles;

namespace DT.DAS.Tests.Tasks;

public sealed class TaskWebApiTests
{
    [Fact]
    public void WebApi_discovers_task_routes_with_data_acquisition_prefix()
    {
        using var factory = TestWebApplicationFactory.Create();
        using var scope = factory.Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IActionDescriptorCollectionProvider>();

        var routes = provider.ActionDescriptors.Items
            .Select(x => x.AttributeRouteInfo?.Template)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("api/data-acquisition/tasks", routes);
        Assert.Contains("api/data-acquisition/tasks/{id:int}", routes);
        Assert.Contains("api/data-acquisition/tasks/mode/{mode:int}", routes);
        Assert.Contains("api/data-acquisition/tasks/status", routes);
        Assert.Contains("api/data-acquisition/tasks/{taskId:int}/groups", routes);
    }

    [Fact]
    public async Task Task_endpoints_return_legacy_wrapped_payloads()
    {
        await using var factory = TestWebApplicationFactory.Create();
        using var client = factory.CreateClient();

        var list = await client.GetAsync("/api/data-acquisition/tasks");
        var detail = await client.GetAsync("/api/data-acquisition/tasks/1");
        var byMode = await client.GetAsync("/api/data-acquisition/tasks/mode/1");
        var groups = await client.GetAsync("/api/data-acquisition/tasks/1/groups");
        var assign = await client.PostAsync("/api/data-acquisition/tasks/1/groups?ids=1,2", new StringContent(string.Empty, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        Assert.Equal(HttpStatusCode.OK, byMode.StatusCode);
        Assert.Equal(HttpStatusCode.OK, groups.StatusCode);
        Assert.Equal(HttpStatusCode.OK, assign.StatusCode);
        Assert.Contains("\"code\":1", await list.Content.ReadAsStringAsync());
        Assert.Contains("\"info\":\"查询成功\"", await detail.Content.ReadAsStringAsync());
        Assert.Contains("\"data\"", await byMode.Content.ReadAsStringAsync());
        Assert.Contains("\"code\":1", await assign.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Swagger_json_contains_task_routes()
    {
        await using var factory = TestWebApplicationFactory.Create(builder => builder.UseEnvironment("Development"));
        using var client = factory.CreateClient();

        var body = await client.GetStringAsync("/swagger/v1/swagger.json");

        Assert.Contains("/api/data-acquisition/tasks", body);
        Assert.Contains("/api/data-acquisition/tasks/{taskId}/groups", body);
    }
}


using System.Net;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using DT.DAS.Tests.TestDoubles;

namespace DT.DAS.Tests.Configs;

public sealed class ConfigWebApiTests
{
    [Fact]
    public void WebApi_discovers_config_routes_with_data_acquisition_prefix_only()
    {
        using var factory = TestWebApplicationFactory.Create();
        using var scope = factory.Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IActionDescriptorCollectionProvider>();

        var routes = provider.ActionDescriptors.Items
            .Select(x => x.AttributeRouteInfo?.Template)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("api/data-acquisition/file-configs", routes);
        Assert.Contains("api/data-acquisition/file-configs/status", routes);
        Assert.Contains("api/data-acquisition/file-configs/group", routes);
        Assert.Contains("api/data-acquisition/file-configs/group/{groupId:int}/configs", routes);
        Assert.DoesNotContain("api/file-configs", routes);
    }

    [Fact]
    public async Task Config_endpoints_return_legacy_wrapped_payloads()
    {
        await using var factory = TestWebApplicationFactory.Create();
        using var client = factory.CreateClient();

        var configs = await client.GetAsync("/api/data-acquisition/file-configs?all=true");
        var groups = await client.GetAsync("/api/data-acquisition/file-configs/group");

        Assert.Equal(HttpStatusCode.OK, configs.StatusCode);
        Assert.Equal(HttpStatusCode.OK, groups.StatusCode);
        var configBody = await configs.Content.ReadAsStringAsync();
        var groupBody = await groups.Content.ReadAsStringAsync();
        Assert.Contains("\"code\":1", configBody);
        Assert.Contains("\"info\":\"查询成功\"", configBody);
        Assert.Contains("\"data\"", groupBody);
    }
}

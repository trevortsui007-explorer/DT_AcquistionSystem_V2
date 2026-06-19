using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using DT.DAS.Tests.TestDoubles;

namespace DT.DAS.Tests.Files;

public sealed class FileWebApiTests
{
    [Fact]
    public void WebApi_discovers_file_routes_with_data_acquisition_prefix()
    {
        using var factory = TestWebApplicationFactory.Create();
        using var scope = factory.Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IActionDescriptorCollectionProvider>();

        var routes = provider.ActionDescriptors.Items
            .Select(x => x.AttributeRouteInfo?.Template)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("api/data-acquisition/files/list", routes);
        Assert.Contains("api/data-acquisition/files/exists", routes);
        Assert.Contains("api/data-acquisition/files/download", routes);
        Assert.Contains("api/data-acquisition/files/upload", routes);
        Assert.Contains("api/data-acquisition/files", routes);
        Assert.Contains("api/data-acquisition/files/preview", routes);
        Assert.Contains("api/data-acquisition/files/discovery", routes);
        Assert.Contains("api/data-acquisition/files/group-discovery", routes);
        Assert.Contains("api/data-acquisition/files/state", routes);
    }

    [Fact]
    public async Task File_query_endpoints_return_legacy_wrapped_payloads()
    {
        await using var factory = TestWebApplicationFactory.Create();
        using var client = factory.CreateClient();
        var tempDir = Path.Combine(Path.GetTempPath(), "dt-das-files-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var csvPath = Path.Combine(tempDir, "input.csv");
        await File.WriteAllTextAsync(csvPath, "Name,Value\none,1\n");

        try
        {
            var list = await client.GetAsync($"/api/data-acquisition/files/list?path={Uri.EscapeDataString(tempDir)}&pattern=*.csv");
            var exists = await client.GetAsync($"/api/data-acquisition/files/exists?path={Uri.EscapeDataString(csvPath)}");
            var preview = await client.GetAsync($"/api/data-acquisition/files/preview?path={Uri.EscapeDataString(csvPath)}&top=1");
            var discovery = await client.GetAsync("/api/data-acquisition/files/discovery?configId=1&startTime=2026-06-18&endTime=2026-06-18");
            var groupDiscovery = await client.GetAsync("/api/data-acquisition/files/group-discovery?groupId=1&date=2026-06-18");
            var state = await client.GetAsync("/api/data-acquisition/files/state?configId=1&businessDate=2026-06-18");

            Assert.Equal(HttpStatusCode.OK, list.StatusCode);
            Assert.Equal(HttpStatusCode.OK, exists.StatusCode);
            Assert.Equal(HttpStatusCode.OK, preview.StatusCode);
            Assert.Equal(HttpStatusCode.OK, discovery.StatusCode);
            Assert.Equal(HttpStatusCode.OK, groupDiscovery.StatusCode);
            Assert.Equal(HttpStatusCode.OK, state.StatusCode);
            Assert.Contains("\"code\":1", await list.Content.ReadAsStringAsync());
            Assert.Contains("\"fileExists\":true", await exists.Content.ReadAsStringAsync());
            Assert.Contains("\"Name\"", await preview.Content.ReadAsStringAsync());
            Assert.Contains("\"data\"", await discovery.Content.ReadAsStringAsync());
            Assert.Contains("\"data\"", await groupDiscovery.Content.ReadAsStringAsync());
            Assert.Contains("\"data\"", await state.Content.ReadAsStringAsync());
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task Swagger_json_contains_file_routes_without_legacy_files_root()
    {
        await using var factory = TestWebApplicationFactory.Create(builder => builder.UseEnvironment("Development"));
        using var client = factory.CreateClient();

        var body = await client.GetStringAsync("/swagger/v1/swagger.json");

        Assert.Contains("/api/data-acquisition/files/list", body);
        Assert.Contains("/api/data-acquisition/files/discovery", body);
        Assert.Contains("/api/data-acquisition/files/state", body);
        Assert.DoesNotContain("/api/files", body);
    }
}

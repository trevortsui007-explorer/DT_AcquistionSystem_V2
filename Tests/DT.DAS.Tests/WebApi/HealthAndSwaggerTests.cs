using System.Net;
using DT.DAS.Application;
using DT.DAS.Application.Acquisition;
using DT.DAS.Application.Tasks;
using DT.DAS.Domain.Interfaces;
using DT.DAS.Infrastructure;
using DT.DAS.Infrastructure.Jobs;
using DT.DAS.Infrastructure.Persistence;
using DT.DAS.Tests.TestDoubles;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DT.DAS.Tests.WebApi;

public sealed class HealthAndSwaggerTests
{
    [Fact]
    public async Task WebApi_health_endpoint_is_available()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void WebApi_container_resolves_core_services()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DAS:Database:DefaultConnectionName"] = "BaseDb",
                ["DAS:Database:ConfigTableName"] = "DA_AcquisitionConfig",
                ["DAS:Database:GroupTableName"] = "DA_AcquisitionGroup",
                ["DAS:Database:GroupConfigTableName"] = "DA_AcquisitionGroup_Config",
                ["DAS:Database:TaskGroupTableName"] = "DA_AcquisitionTask_Group",
                ["DAS:Database:TaskTableName"] = "DA_AcquisitionTask",
                ["DAS:Hangfire:Enabled"] = "false",
                ["DAS:Hangfire:ConnectionName"] = "BaseDb"
            })
            .Build();

        var services = new ServiceCollection()
            .AddDasApplication()
            .AddDasInfrastructure(configuration)
            .BuildServiceProvider(validateScopes: true);

        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        Assert.NotNull(provider.GetRequiredService<IAcquisitionExecutionService>());
        Assert.NotNull(provider.GetRequiredService<IAcquisitionTaskService>());
        Assert.NotNull(provider.GetRequiredService<IFileProviderFactory>());
        Assert.NotNull(provider.GetRequiredService<IDataParserFactory>().Create("input.csv"));
        Assert.NotNull(provider.GetRequiredService<IDataParserFactory>().Create("input.xlsx"));
        Assert.NotNull(provider.GetRequiredService<IAcquisitionJobScheduler>() as NoopAcquisitionJobScheduler);
        Assert.NotNull(provider.GetRequiredService<ISqlConnectionFactory>());
        Assert.Equal(2, provider.GetServices<IFileProvider>().Count());
        Assert.Equal(2, provider.GetServices<IDataParserDescriptor>().Count());
    }

    [Fact]
    public void WebApi_discovers_acquisition_execution_routes()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IActionDescriptorCollectionProvider>();

        var routes = provider.ActionDescriptors.Items
            .Select(x => x.AttributeRouteInfo?.Template)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("api/data-acquisition/execution/start/by-ids", routes);
        Assert.Contains("api/data-acquisition/execution/{taskLogId}/status", routes);
    }

    [Fact]
    public async Task Swagger_json_is_available_in_development()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("AcquisitionExecution", body);
    }
}

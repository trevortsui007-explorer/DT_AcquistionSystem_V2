using DT.DAS.Domain.Entities;
using DT.DAS.Domain.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DT.DAS.Tests.TestDoubles;

internal static class TestWebApplicationFactory
{
    public static WebApplicationFactory<Program> Create(Action<IWebHostBuilder>? configure = null)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                configure?.Invoke(builder);
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IFileConfigRepository>();
                    services.RemoveAll<IFileConfigGroupRepository>();
                    services.RemoveAll<IAcquisitionTaskRepository>();
                    services.RemoveAll<IAcquisitionLogRepository>();
                    services.AddSingleton<IFileConfigRepository>(new InMemoryFileConfigRepository(new AcquisitionConfig { Id = 1, EqName = "EQ-1", IsEnabled = true }));
                    services.AddSingleton<IFileConfigGroupRepository>(new InMemoryFileConfigGroupRepository());
                    services.AddSingleton<IAcquisitionTaskRepository>(new RecordingAcquisitionTaskRepository());
                    services.AddSingleton<IAcquisitionLogRepository>(new InMemoryAcquisitionLogRepository());
                });
            });
    }
}


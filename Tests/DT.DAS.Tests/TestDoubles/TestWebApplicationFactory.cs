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
                    services.RemoveAll<IAcquisitionFileStateRepository>();
                    services.RemoveAll<IDataService>();
                    services.AddSingleton<IFileConfigRepository>(new InMemoryFileConfigRepository(new AcquisitionConfig { Id = 1, EqName = "EQ-1", FilePathPattern = "C:/missing/{yyyy}/{MM}", FileNamePattern = "input_{yyyy}{MM}{dd}", FileType = ".csv", IsEnabled = true }));
                    services.AddSingleton<IFileConfigGroupRepository>(new InMemoryFileConfigGroupRepository());
                    services.AddSingleton<IAcquisitionTaskRepository>(new RecordingAcquisitionTaskRepository());
                    services.AddSingleton<IAcquisitionLogRepository>(new InMemoryAcquisitionLogRepository());
                    services.AddSingleton<IAcquisitionFileStateRepository>(new InMemoryAcquisitionFileStateRepository());
                    services.AddSingleton<IDataService>(new RecordingDataService());
                });
            });
    }
}




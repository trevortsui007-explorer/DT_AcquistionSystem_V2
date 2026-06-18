using DT.DAS.Application.Acquisition;
using DT.DAS.Application.Acquisition.Services;
using DT.DAS.Application.Configs;
using DT.DAS.Application.Configs.Services;
using DT.DAS.Application.PostProcessing;
using DT.DAS.Application.PostProcessing.Services;
using DT.DAS.Application.Tasks;
using DT.DAS.Application.Tasks.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DT.DAS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddDasApplication(this IServiceCollection services)
    {
        services.AddScoped<IFileConfigService, FileConfigService>();
        services.AddScoped<IAcquisitionLogService, AcquisitionLogService>();
        services.AddScoped<IAcquisitionFileStateService, AcquisitionFileStateService>();
        services.AddScoped<IDataAcquisitionService, DataAcquisitionService>();
        services.AddScoped<IAcquisitionExecutionService, AcquisitionExecutionService>();
        services.AddScoped<IPostProcessingService, PostProcessingService>();
        services.AddScoped<IPostProcessorResolver, PostProcessorResolver>();
        services.AddScoped<ILogCodeGenerator, LogCodeGenerator>();

        return services;
    }
}

using DT.DAS.Application.Acquisition;
using DT.DAS.Application.Acquisition.Services;
using DT.DAS.Application.Configs;
using DT.DAS.Application.Configs.Services;
using DT.DAS.Application.Data;
using DT.DAS.Application.Data.Services;
using DT.DAS.Application.Files;
using DT.DAS.Application.Files.Services;
using DT.DAS.Application.PostProcessing;
using DT.DAS.Application.Reports;
using DT.DAS.Application.Reports.Services;
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
        services.AddScoped<IFileConfigGroupService, FileConfigGroupService>();
        services.AddScoped<IAcquisitionTaskService, AcquisitionTaskService>();
        services.AddScoped<IDataMaintenanceService, DataMaintenanceService>();
        services.AddScoped<IFileAccessService, FileAccessService>();
        services.AddScoped<IDataPreviewService, DataPreviewService>();
        services.AddScoped<IFileDiscoveryService, FileDiscoveryService>();
        services.AddScoped<IAcquisitionLogService, AcquisitionLogService>();
        services.AddScoped<IAcquisitionFileStateService, AcquisitionFileStateService>();
        services.AddScoped<IDataAcquisitionService, DataAcquisitionService>();
        services.AddScoped<IAcquisitionExecutionService, AcquisitionExecutionService>();
        services.AddScoped<IReportExportService, ReportExportService>();
        services.AddScoped<IPostProcessingService, PostProcessingService>();
        services.AddScoped<IPostProcessorResolver, PostProcessorResolver>();
        services.AddScoped<ILogCodeGenerator, LogCodeGenerator>();

        return services;
    }
}







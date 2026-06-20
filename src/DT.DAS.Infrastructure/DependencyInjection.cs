using DT.DAS.Application;
using DT.DAS.Application.Acquisition;
using DT.DAS.Application.Reports;
using DT.DAS.Domain.Interfaces;
using DT.DAS.Infrastructure.Export;
using DT.DAS.Infrastructure.FileAccess.Providers;
using DT.DAS.Infrastructure.Jobs;
using DT.DAS.Infrastructure.Options;
using DT.DAS.Infrastructure.Parsing.Parsers;
using DT.DAS.Infrastructure.Persistence;
using DT.DAS.Infrastructure.Persistence.Repositories;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DT.DAS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDasCore(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddDasApplication()
            .AddDasInfrastructure(configuration);
    }

    public static IServiceCollection AddDasInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(configuration);
        services.AddDasOptions(configuration);
        services.AddPersistence();
        services.AddFileAccess();
        services.AddParsing();
        services.AddBackgroundJobs(configuration);

        return services;
    }

    private static IServiceCollection AddDasOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DasDatabaseOptions>()
            .Bind(configuration.GetSection("DAS:Database"))
            .Validate(x => !string.IsNullOrWhiteSpace(x.DefaultConnectionName), "DAS:Database:DefaultConnectionName is required.")
            .Validate(x => !string.IsNullOrWhiteSpace(x.ConfigTableName), "DAS:Database:ConfigTableName is required.")
            .Validate(x => !string.IsNullOrWhiteSpace(x.GroupTableName), "DAS:Database:GroupTableName is required.")
            .Validate(x => !string.IsNullOrWhiteSpace(x.GroupConfigTableName), "DAS:Database:GroupConfigTableName is required.")
            .Validate(x => !string.IsNullOrWhiteSpace(x.TaskGroupTableName), "DAS:Database:TaskGroupTableName is required.")
            .Validate(x => !string.IsNullOrWhiteSpace(x.TaskTableName), "DAS:Database:TaskTableName is required.")
            .ValidateOnStart();

        services.AddOptions<HangfireOptions>()
            .Bind(configuration.GetSection("DAS:Hangfire"))
            .Validate(x => !x.Enabled || !string.IsNullOrWhiteSpace(x.ConnectionName), "DAS:Hangfire:ConnectionName is required when Hangfire is enabled.")
            .ValidateOnStart();

        return services;
    }

    private static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
        services.AddScoped<IFileConfigRepository, FileConfigRepository>();
        services.AddScoped<IFileConfigGroupRepository, FileConfigGroupRepository>();
        services.AddScoped<IAcquisitionTaskRepository, AcquisitionTaskRepository>();
        services.AddScoped<IAcquisitionLogRepository, AcquisitionLogRepository>();
        services.AddScoped<IAcquisitionFileStateRepository, AcquisitionFileStateRepository>();
        services.AddScoped<IDataService, SqlDataService>();
        services.AddScoped<IReportExportTaskRepository, ReportExportTaskRepository>();
        services.AddScoped<IReportExportDataProvider, ReportExportDataProvider>();
        services.AddSingleton<IReportWorkbookWriter, NpoiReportWorkbookWriter>();
        services.AddSingleton<IReportArchiveService, ReportArchiveService>();

        return services;
    }

    private static IServiceCollection AddFileAccess(this IServiceCollection services)
    {
        services.AddSingleton<LocalFileProvider>();
        services.AddSingleton<FtpFileProvider>();
        services.AddSingleton<IFileProvider>(sp => sp.GetRequiredService<LocalFileProvider>());
        services.AddSingleton<IFileProvider>(sp => sp.GetRequiredService<FtpFileProvider>());
        services.AddSingleton<IFileProviderFactory, FileProviderFactory>();

        return services;
    }

    private static IServiceCollection AddParsing(this IServiceCollection services)
    {
        services.AddSingleton<CsvStreamParser>();
        services.AddSingleton<ExcelStreamParser>();
        services.AddSingleton<IDataParserDescriptor>(sp => new DataParserDescriptor(sp.GetRequiredService<CsvStreamParser>(), new[] { ".csv", ".txt" }));
        services.AddSingleton<IDataParserDescriptor>(sp => new DataParserDescriptor(sp.GetRequiredService<ExcelStreamParser>(), new[] { ".xls", ".xlsx" }));
        services.AddSingleton<IDataParserFactory, DataParserFactory>();

        return services;
    }

    private static IServiceCollection AddBackgroundJobs(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<AcquisitionJob>();
        services.AddScoped<ReportExportJob>();

        var hangfireSection = configuration.GetSection("DAS:Hangfire").Get<HangfireOptions>() ?? new HangfireOptions();
        var hangfireConnection = configuration.GetConnectionString(hangfireSection.ConnectionName);
        if (hangfireSection.Enabled && !string.IsNullOrWhiteSpace(hangfireConnection))
        {
            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(hangfireConnection, new SqlServerStorageOptions
                {
                    PrepareSchemaIfNecessary = true
                }));

            services.AddHangfireServer();
            services.AddScoped<IAcquisitionJobScheduler, HangfireAcquisitionJobScheduler>();
            services.AddScoped<IReportExportJobScheduler, HangfireReportExportJobScheduler>();
        }
        else
        {
            services.AddScoped<IAcquisitionJobScheduler, NoopAcquisitionJobScheduler>();
            services.AddScoped<IReportExportJobScheduler, NoopReportExportJobScheduler>();
        }

        return services;
    }
}





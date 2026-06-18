using System.Net;
using System.Text;
using DT.DAS.Application;
using DT.DAS.Application.Acquisition;
using DT.DAS.Application.Configs;
using DT.DAS.Application.PostProcessing;
using DT.DAS.Application.PostProcessing.Contracts;
using DT.DAS.Application.Tasks;
using DT.DAS.Application.Acquisition.Services;
using DT.DAS.Application.Configs.Services;
using DT.DAS.Application.PostProcessing.Services;
using DT.DAS.Application.Tasks.Services;
using DT.DAS.Application.Acquisition.Utilities;
using DT.DAS.Domain.Entities;
using DT.DAS.Domain.Interfaces;
using DT.DAS.Infrastructure.FileAccess.Providers;
using DT.DAS.Infrastructure.Parsing.Parsers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using DT.DAS.Infrastructure;
using DT.DAS.Infrastructure.Jobs;
using DT.DAS.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DT.DAS.Tests;

public sealed class CoreInfrastructureTests
{
    [Fact]
    public async Task CsvParser_keeps_source_metadata_and_start_row()
    {
        var parser = new CsvStreamParser();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Name,Value\nskip,0\nalpha,42\n"));

        var rows = await parser.ParseAsync<Dictionary<string, object?>>(stream, new CsvParserOptions
        {
            HeaderRow = 1,
            StartRow = 3,
            HasExtFields = true,
            FilePath = "sample.csv"
        });

        Assert.Single(rows);
        Assert.Equal("alpha", rows[0]["Name"]);
        Assert.Equal("42", rows[0]["Value"]);
        Assert.Equal("sample.csv", rows[0]["fullFilePath"]);
        Assert.Equal(3, rows[0]["row"]);
    }

    [Fact]
    public void FileProviderFactory_routes_by_protocol()
    {
        var local = new FakeFileProvider("local", path => !path.Contains("://", StringComparison.Ordinal));
        var ftp = new FakeFileProvider("ftp", path => path.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase));
        var factory = new FileProviderFactory(new IFileProvider[] { local, ftp });

        Assert.Same(local, factory.Create("C:\\data\\a.csv"));
        Assert.Same(ftp, factory.Create("ftp://server/a.csv"));
    }

    [Fact]
    public void DataMapperUtil_applies_json_field_mappings()
    {
        var mapped = DataMapperUtil.MapRow(
            new Dictionary<string, object?> { ["SourceName"] = "A1", ["Value"] = 12 },
            """{"SourceName":"EqName"}""");

        Assert.Equal("A1", mapped["EqName"]);
        Assert.Equal(12, mapped["Value"]);
    }

    [Fact]
    public void AcquisitionLogService_normalizes_status_and_progress()
    {
        Assert.Equal("PartialSuccess", AcquisitionLogService.NormalizeStatus("partial_success"));
        Assert.Equal(66, AcquisitionLogService.CalculateProgress(3, 1, 1));
        Assert.Equal(100, AcquisitionLogService.CalculateProgress(2, 2, 1));
    }

    [Fact]
    public async Task AcquisitionExecutionService_creates_task_log_and_enqueues_job()
    {
        var configRepository = new InMemoryFileConfigRepository(new AcquisitionConfig { Id = 7, EqName = "EQ-7", IsEnabled = true });
        var logRepository = new InMemoryAcquisitionLogRepository();
        var logService = new AcquisitionLogService(configRepository, logRepository);
        var scheduler = new RecordingScheduler();
        var service = new AcquisitionExecutionService(
            new FileConfigService(configRepository),
            logService,
            new LogCodeGenerator(),
            scheduler);

        var result = await service.StartByIdsAsync(new[] { "7" }, new DateTime(2026, 6, 18));

        Assert.Equal("Running", result.Status);
        Assert.Equal("1", result.TaskLogId);
        Assert.Equal("1", scheduler.TaskLogId);
        Assert.Equal(new[] { 7 }, scheduler.ConfigIds);
    }

    [Fact]
    public async Task WebApi_health_endpoint_is_available()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void WebApi_container_resolves_core_execution_service()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IAcquisitionExecutionService>();

        Assert.NotNull(service);
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


    [Fact]
    public void Das_ioc_resolves_core_services_and_extension_points()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DAS:Database:DefaultConnectionName"] = "BaseDb",
                ["DAS:Database:ConfigTableName"] = "DA_AcquisitionConfig",
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
        Assert.NotNull(provider.GetRequiredService<IFileProviderFactory>());
        Assert.NotNull(provider.GetRequiredService<IDataParserFactory>().Create("input.csv"));
        Assert.NotNull(provider.GetRequiredService<IDataParserFactory>().Create("input.xlsx"));
        Assert.NotNull(provider.GetRequiredService<IAcquisitionJobScheduler>() as NoopAcquisitionJobScheduler);
        Assert.NotNull(provider.GetRequiredService<ISqlConnectionFactory>());
        Assert.Equal(2, provider.GetServices<IFileProvider>().Count());
        Assert.Equal(2, provider.GetServices<IDataParserDescriptor>().Count());
    }

    [Fact]
    public void Project_structure_uses_module_folders_and_sql_scripts()
    {
        var root = FindSolutionRoot();

        Assert.True(Directory.Exists(Path.Combine(root, "src", "DT.DAS.Application", "Acquisition")));
        Assert.True(Directory.Exists(Path.Combine(root, "src", "DT.DAS.Application", "Configs")));
        Assert.True(Directory.Exists(Path.Combine(root, "src", "DT.DAS.Application", "Tasks")));
        Assert.True(Directory.Exists(Path.Combine(root, "src", "DT.DAS.Application", "PostProcessing")));
        Assert.True(Directory.Exists(Path.Combine(root, "src", "DT.DAS.Infrastructure", "Persistence", "Scripts", "Tasks")));
        Assert.True(File.Exists(Path.Combine(root, "src", "DT.DAS.Infrastructure", "Persistence", "Scripts", "Tasks", "001_create_acquisition_task_logs.sql")));
        Assert.False(File.Exists(Path.Combine(root, "src", "DT.DAS.Application", "Services", "ServiceContracts.cs")));
    }

    private static string FindSolutionRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null && !File.Exists(Path.Combine(current.FullName, "DT_DataAcquisitionSystem.sln")))
        {
            current = current.Parent;
        }

        return current?.FullName ?? throw new DirectoryNotFoundException("Could not find solution root.");
    }
    private sealed class FakeFileProvider : IFileProvider
    {
        private readonly Func<string, bool> _canHandle;

        public FakeFileProvider(string name, Func<string, bool> canHandle)
        {
            Name = name;
            _canHandle = canHandle;
        }

        public string Name { get; }
        public bool CanHandle(string path) => _canHandle(path);
        public bool Exists(string filePath) => true;
        public Task<Stream> GetFileStreamAsync(string filePath, CancellationToken cancellationToken = default) => Task.FromResult<Stream>(Stream.Null);
        public Task SaveFileAsync(string filePath, Stream content, bool overwrite = true, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IEnumerable<string>> GetFileNamesAsync(string directoryPath, string searchPattern = "*.*", bool recursive = false, CancellationToken cancellationToken = default) => Task.FromResult(Enumerable.Empty<string>());
        public Task MoveFileAsync(string sourcePath, string destinationPath, bool overwrite = false, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class RecordingScheduler : IAcquisitionJobScheduler
    {
        public string? TaskLogId { get; private set; }
        public IReadOnlyCollection<int> ConfigIds { get; private set; } = Array.Empty<int>();

        public string Enqueue(string taskLogId, IReadOnlyCollection<int> configIds, DateTime startDate, DateTime endDate, string updateSource, bool sealOnSuccess)
        {
            TaskLogId = taskLogId;
            ConfigIds = configIds.ToArray();
            return "job-1";
        }
    }

    private sealed class InMemoryFileConfigRepository : IFileConfigRepository
    {
        private readonly List<AcquisitionConfig> _configs;

        public InMemoryFileConfigRepository(params AcquisitionConfig[] configs)
        {
            _configs = configs.ToList();
        }

        public IEnumerable<AcquisitionConfig> GetListByIds(IEnumerable<string> ids, string? tableName = null, string? databaseName = null)
        {
            var idSet = ids.Select(int.Parse).ToHashSet();
            return _configs.Where(x => idSet.Contains(x.Id));
        }

        public IEnumerable<AcquisitionConfig> GetListByGroupIds(IEnumerable<string> groupIds, string? tableName = null, string? linkTableName = null, string? databaseName = null) => _configs;
        public IEnumerable<AcquisitionConfig> GetListByTaskIds(IEnumerable<string> taskIds, string? tableName = null, string? databaseName = null) => _configs;
        public IEnumerable<AcquisitionConfig> GetList(FileConfigQueryOptions? options = null) => _configs;
    }

    private sealed class InMemoryAcquisitionLogRepository : IAcquisitionLogRepository
    {
        private int _nextId;

        public Task<string?> InsertAsync(AcquisitionLogEntry entry, CancellationToken ct = default)
        {
            entry.Id = Interlocked.Increment(ref _nextId).ToString();
            return Task.FromResult<string?>(entry.Id);
        }

        public Task<string?> InsertAsync(AcquisitionTaskLogEntry entry, CancellationToken ct = default)
        {
            entry.Id = Interlocked.Increment(ref _nextId).ToString();
            return Task.FromResult<string?>(entry.Id);
        }

        public Task<bool> UpdateAsync(AcquisitionTaskLogEntry entry, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> UpdateProgressAsync(AcquisitionTaskLogEntry entry, CancellationToken ct = default) => Task.FromResult(true);
        public Task<AcquisitionTaskLogEntry?> GetTaskLogByIdAsync(string taskLogId, CancellationToken ct = default) => Task.FromResult<AcquisitionTaskLogEntry?>(null);
        public Task<List<AcquisitionLogEntry>> GetLogsByTaskLogIdAsync(string taskLogId, CancellationToken ct = default) => Task.FromResult(new List<AcquisitionLogEntry>());
        public Task<int> GetLastProcessedRowByConfigIdAsync(int configId, string fileName, CancellationToken ct = default) => Task.FromResult(0);
        public Task<List<AcquisitionTaskLogEntry>> GetTaskLogsAsync(int pageNo, int pageSize, string? status = null, DateTime? startTime = null, DateTime? endTime = null, int? taskId = null, CancellationToken ct = default) => Task.FromResult(new List<AcquisitionTaskLogEntry>());
        public Task<int> GetTaskLogsCountAsync(string? status = null, DateTime? startTime = null, DateTime? endTime = null, int? taskId = null, CancellationToken ct = default) => Task.FromResult(0);
    }
}






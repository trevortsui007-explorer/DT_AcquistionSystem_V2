using System.Text;
using DT.DAS.Application.Configs.Services;
using DT.DAS.Application.Files.Services;
using DT.DAS.Application.Tasks.Services;
using DT.DAS.Domain.Entities;
using DT.DAS.Domain.Interfaces;
using DT.DAS.Infrastructure.FileAccess.Providers;
using DT.DAS.Infrastructure.Parsing.Parsers;
using DT.DAS.Tests.TestDoubles;

namespace DT.DAS.Tests.Files;

public sealed class FileServiceTests
{
    [Fact]
    public async Task FileAccessService_routes_provider_and_supports_basic_operations()
    {
        var provider = new FakeFileProvider("local", path => !path.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase));
        provider.AddFile("C:/data/a.csv", "Name\nalpha");
        var service = new FileAccessService(new FileProviderFactory(new IFileProvider[] { provider }));

        var files = await service.GetFileNamesAsync("C:/data", "*.csv");
        var exists = service.Exists("C:/data/a.csv");
        await service.DeleteAsync("C:/data/a.csv");

        Assert.Contains("C:/data/a.csv", files);
        Assert.True(exists);
        Assert.Contains("C:/data/a.csv", provider.DeletedFiles);
        Assert.Equal("*.csv", provider.LastSearchPattern);
    }

    [Fact]
    public async Task DataPreviewService_uses_parser_factory_and_caps_top()
    {
        var provider = new FakeFileProvider("local", _ => true);
        provider.AddFile("C:/data/a.csv", "Name,Value\none,1\ntwo,2\n");
        var factory = new FileProviderFactory(new IFileProvider[] { provider });
        var parserFactory = new DataParserFactory(new IDataParserDescriptor[] { new DataParserDescriptor(new CsvStreamParser(), new[] { ".csv" }) });
        var service = new DataPreviewService(factory, parserFactory);

        var rows = await service.GetFilePreviewAsync("C:/data/a.csv", 1);

        Assert.Single(rows);
        Assert.Equal("one", rows[0]["Name"]);
        Assert.Equal(200, DataPreviewService.NormalizeTop(999));
        Assert.Equal(10, DataPreviewService.NormalizeTop(0));
    }

    [Fact]
    public async Task FileDiscoveryService_marks_existing_and_missing_files_and_attaches_state()
    {
        var provider = new FakeFileProvider("local", _ => true);
        provider.AddFile("C:/data/2026/06/input_20260618.csv", "Name\nalpha");
        var config = new AcquisitionConfig
        {
            Id = 7,
            EqName = "EQ-7",
            FilePathPattern = "C:/data/{yyyy}/{MM}",
            FileNamePattern = "input_{yyyy}{MM}{dd}",
            FileType = ".csv"
        };
        var configRepository = new InMemoryFileConfigRepository(config);
        var stateRepository = new InMemoryAcquisitionFileStateRepository();
        stateRepository.Add(new AcquisitionFileState
        {
            ConfigId = 7,
            BusinessDate = new DateTime(2026, 6, 18),
            FileName = "input_20260618.csv",
            DataRowCount = 12,
            LastStartRow = 2,
            LastProcessedRows = 10,
            LastStatus = "Success",
            LastUpdateSource = "MANUAL_CURRENT",
            IsSealed = true,
            UpdateTime = new DateTime(2026, 6, 19)
        });
        var service = new FileDiscoveryService(
            new FileProviderFactory(new IFileProvider[] { provider }),
            new FileConfigService(configRepository),
            new AcquisitionFileStateService(stateRepository));

        var result = await service.GetDetailedDiscoveryAsync(config, new DateTime(2026, 6, 18), new DateTime(2026, 6, 19));
        var files = result.Single().Files;

        Assert.Equal(2, files.Count);
        Assert.False(files[0].IsMissing);
        Assert.True(files[1].IsMissing);
        Assert.Equal(12, files[0].DataRowCount);
        Assert.True(files[0].IsSealed);
        Assert.Equal(new DateTime(2026, 6, 18), stateRepository.LastStartDate);
        Assert.Equal(new DateTime(2026, 6, 19), stateRepository.LastEndDate);
    }

    [Fact]
    public async Task FileStateService_returns_empty_for_invalid_config_and_rejects_invalid_range()
    {
        var repository = new InMemoryAcquisitionFileStateRepository();
        var service = new AcquisitionFileStateService(repository);

        Assert.Empty(await service.GetByConfigAndDateRangeAsync(0, DateTime.Today, DateTime.Today));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetByConfigAndDateRangeAsync(1, DateTime.Today, DateTime.Today.AddDays(-1)));
    }
}

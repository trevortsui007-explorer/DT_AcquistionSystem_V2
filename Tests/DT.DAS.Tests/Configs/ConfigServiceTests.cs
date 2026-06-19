using DT.DAS.Application.Acquisition.Utilities;
using DT.DAS.Application.Configs.Services;
using DT.DAS.Domain.Entities;
using DT.DAS.Tests.TestDoubles;

namespace DT.DAS.Tests.Configs;

public sealed class ConfigServiceTests
{
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
    public void FileConfigService_routes_filters_and_guards_empty_mutations()
    {
        var repository = new RecordingFileConfigRepository(new AcquisitionConfig { Id = 1, EqName = "EQ-1" });
        var service = new FileConfigService(repository);

        Assert.Single(service.GetFileConfigs(new FileConfigQueryOptions { Ids = new[] { "1" } }));
        Assert.Equal("ids", repository.LastReadRoute);

        Assert.Single(service.GetFileConfigs(new FileConfigQueryOptions { GroupIds = new[] { "2" } }));
        Assert.Equal("groups", repository.LastReadRoute);

        Assert.Single(service.GetFileConfigs(new FileConfigQueryOptions { TaskIds = new[] { "3" } }));
        Assert.Equal("tasks", repository.LastReadRoute);

        var paged = service.GetFileConfigsPaged(new FileConfigQueryOptions(), 0, 0);
        Assert.Equal(1, paged.Total);
        Assert.Equal((1, 10), repository.LastPageRequest);
        Assert.False(service.DeleteConfigs(Array.Empty<string>()));
        Assert.False(service.SetEnabledStatus(Array.Empty<string>(), true));
    }
}

using DT.DAS.Application.Acquisition.Services;
using DT.DAS.Application.Configs.Services;
using DT.DAS.Application.Tasks.Services;
using DT.DAS.Domain.Entities;
using DT.DAS.Tests.TestDoubles;

namespace DT.DAS.Tests.Acquisition;

public sealed class AcquisitionExecutionTests
{
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
    public void AcquisitionLogService_normalizes_status_and_progress()
    {
        Assert.Equal("PartialSuccess", AcquisitionLogService.NormalizeStatus("partial_success"));
        Assert.Equal(66, AcquisitionLogService.CalculateProgress(3, 1, 1));
        Assert.Equal(100, AcquisitionLogService.CalculateProgress(2, 2, 1));
    }
}

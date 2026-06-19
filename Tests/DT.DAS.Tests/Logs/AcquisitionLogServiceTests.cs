using DT.DAS.Application.Tasks.Services;
using DT.DAS.Domain.Entities;
using DT.DAS.Tests.TestDoubles;

namespace DT.DAS.Tests.Logs;

public sealed class AcquisitionLogServiceTests
{
    [Fact]
    public async Task GetNextStartRowAsync_prefers_log_checkpoint_then_config_start_row()
    {
        var configRepository = new InMemoryFileConfigRepository(new AcquisitionConfig { Id = 7, StartRow = 3 });
        var logRepository = new InMemoryAcquisitionLogRepository();
        var service = new AcquisitionLogService(configRepository, logRepository);

        logRepository.SetNextStartRow(7, "input.csv", 42);

        Assert.Equal(42, await service.GetNextStartRowAsync(7, "input.csv"));
        Assert.Equal(3, await service.GetNextStartRowAsync(7, "new.csv"));
    }

    [Fact]
    public async Task RecordLogEntryAsync_validates_required_fields_and_normalizes_status()
    {
        var service = CreateService(out var logRepository);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RecordLogEntryAsync(new AcquisitionLogEntry { ConfigId = 0, TaskLogId = "1" }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RecordLogEntryAsync(new AcquisitionLogEntry { ConfigId = 1, TaskLogId = " " }));

        var id = await service.RecordLogEntryAsync(new AcquisitionLogEntry { ConfigId = 1, TaskLogId = " 9 ", FileName = " input.csv ", Status = "failed" });

        Assert.Equal("1", id);
        Assert.Equal("Failed", logRepository.LastInsertedLog?.Status);
        Assert.Equal("9", logRepository.LastInsertedLog?.TaskLogId);
        Assert.Equal("input.csv", logRepository.LastInsertedLog?.FileName);
    }

    [Fact]
    public async Task RecordTaskLogEntryAsync_validates_trigger_and_calculates_progress()
    {
        var service = CreateService(out var logRepository);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RecordTaskLogEntryAsync(new AcquisitionTaskLogEntry { TriggerType = "AUTO" }));

        var id = await service.RecordTaskLogEntryAsync(new AcquisitionTaskLogEntry
        {
            TaskId = 3,
            TaskCode = " TASK-1 ",
            TriggerType = "man",
            Status = "partial_success",
            TotalConfigs = 3,
            SuccessCount = 1,
            FailureCount = 1
        });

        Assert.Equal("1", id);
        Assert.Equal("TASK-1", logRepository.LastInsertedTaskLog?.TaskCode);
        Assert.Equal("MAN", logRepository.LastInsertedTaskLog?.TriggerType);
        Assert.Equal("PartialSuccess", logRepository.LastInsertedTaskLog?.Status);
        Assert.Equal(2, logRepository.LastInsertedTaskLog?.ProcessedCount);
        Assert.Equal(66, logRepository.LastInsertedTaskLog?.Progress);
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_updates_existing_rows_and_returns_false_for_missing_rows()
    {
        var service = CreateService(out var logRepository);
        var id = await service.RecordTaskLogEntryAsync(new AcquisitionTaskLogEntry { TaskId = 1, TriggerType = "SCH", TotalConfigs = 1 });

        var success = await service.UpdateTaskStatusAsync(id!, "success", 1);
        var missing = await service.UpdateTaskStatusAsync("missing", "success", 1);

        Assert.True(success);
        Assert.False(missing);
        Assert.Equal("Success", logRepository.LastUpdatedTaskLog?.Status);
        Assert.Equal(1, logRepository.LastUpdatedTaskLog?.SuccessCount);
        Assert.Equal(100, logRepository.LastUpdatedTaskLog?.Progress);
        Assert.NotNull(logRepository.LastUpdatedTaskLog?.EndTime);
    }

    [Fact]
    public async Task GetTaskLogsAsync_normalizes_paging_and_status_filter()
    {
        var service = CreateService(out var logRepository);

        await service.GetTaskLogsAsync(0, 999, "failed", taskId: 5);

        Assert.Equal(1, logRepository.LastPageNo);
        Assert.Equal(200, logRepository.LastPageSize);
        Assert.Equal("Failed", logRepository.LastStatusFilter);
        Assert.Equal(5, logRepository.LastTaskIdFilter);
    }

    private static AcquisitionLogService CreateService(out InMemoryAcquisitionLogRepository logRepository)
    {
        var configRepository = new InMemoryFileConfigRepository(new AcquisitionConfig { Id = 1, StartRow = 2 });
        logRepository = new InMemoryAcquisitionLogRepository();
        return new AcquisitionLogService(configRepository, logRepository);
    }
}

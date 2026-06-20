using DT.DAS.Application.Reports.Contracts;
using DT.DAS.Application.Reports.Services;
using DT.DAS.Domain.Entities;
using DT.DAS.Domain.Interfaces;
using DT.DAS.Tests.TestDoubles;

namespace DT.DAS.Tests.Reports;

public sealed class ReportExportServiceTests
{
    [Fact]
    public async Task CreateTaskAsync_validates_request()
    {
        var service = CreateService(out _, out _, out _, out _, out _);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateTaskAsync(null));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateTaskAsync(new ReportExportCreateRequestDto { GroupIds = new List<int>(), StartTime = DateTime.Today, EndTime = DateTime.Today }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateTaskAsync(new ReportExportCreateRequestDto { GroupIds = new List<int> { 1 }, StartTime = DateTime.Today, EndTime = DateTime.Today.AddDays(-1) }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateTaskAsync(new ReportExportCreateRequestDto { GroupIds = new List<int> { 1 }, StartTime = DateTime.Today, EndTime = DateTime.Today.AddDays(8) }));
    }

    [Fact]
    public async Task CreateTaskAsync_deduplicates_groups_and_queues_without_execution()
    {
        var service = CreateService(out var repository, out var dataProvider, out _, out _, out var scheduler);
        dataProvider.Groups.Add(new ReportExportGroupDefinition { GroupId = 1, GroupName = "G-1", ExportProcedureName = "dbo.ExportG1" });
        dataProvider.Groups.Add(new ReportExportGroupDefinition { GroupId = 2, GroupName = "G-2", ExportProcedureName = "dbo.ExportG2" });

        var result = await service.CreateTaskAsync(new ReportExportCreateRequestDto
        {
            GroupIds = new List<int> { 2, 1, 1 },
            StartTime = new DateTime(2026, 6, 1),
            EndTime = new DateTime(2026, 6, 2)
        });

        Assert.False(string.IsNullOrWhiteSpace(result.ExportTaskId));
        Assert.Equal("1,2", repository.LastCreated?.GroupIds);
        Assert.Equal(ReportExportTaskStatus.Queued, repository.LastCreated?.Status);
        Assert.Equal(result.ExportTaskId, scheduler.LastTaskId);
    }

    [Fact]
    public async Task CreateTaskAsync_rejects_missing_groups_and_groups_without_procedure()
    {
        var service = CreateService(out _, out var dataProvider, out _, out _, out _);
        dataProvider.Groups.Add(new ReportExportGroupDefinition { GroupId = 1, GroupName = "G-1", ExportProcedureName = "" });

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateTaskAsync(new ReportExportCreateRequestDto { GroupIds = new List<int> { 1 }, StartTime = DateTime.Today, EndTime = DateTime.Today }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateTaskAsync(new ReportExportCreateRequestDto { GroupIds = new List<int> { 2 }, StartTime = DateTime.Today, EndTime = DateTime.Today }));
    }

    [Fact]
    public async Task ExecuteTaskAsync_writes_single_excel_and_marks_success()
    {
        var service = CreateService(out var repository, out var dataProvider, out var writer, out _, out _);
        dataProvider.Groups.Add(new ReportExportGroupDefinition { GroupId = 1, GroupName = "G-1", ExportProcedureName = "dbo.ExportG1" });
        repository.Add(NewTask("task-1", "1"));

        await service.ExecuteTaskAsync("task-1");
        var task = await repository.GetByIdAsync("task-1");

        Assert.Equal(ReportExportTaskStatus.Success, task?.Status);
        Assert.Equal(100, task?.Progress);
        Assert.Single(writer.WrittenFiles);
        Assert.EndsWith(".xlsx", task?.FileName);
    }

    [Fact]
    public async Task ExecuteTaskAsync_writes_zip_for_multiple_groups_and_marks_failed_on_error()
    {
        var service = CreateService(out var repository, out var dataProvider, out _, out var archive, out _);
        dataProvider.Groups.Add(new ReportExportGroupDefinition { GroupId = 1, GroupName = "G-1", ExportProcedureName = "dbo.ExportG1" });
        dataProvider.Groups.Add(new ReportExportGroupDefinition { GroupId = 2, GroupName = "G-2", ExportProcedureName = "dbo.ExportG2" });
        repository.Add(NewTask("task-zip", "1,2"));

        await service.ExecuteTaskAsync("task-zip");
        var zipTask = await repository.GetByIdAsync("task-zip");

        Assert.Equal(ReportExportTaskStatus.Success, zipTask?.Status);
        Assert.EndsWith(".zip", zipTask?.FileName);
        Assert.Equal(zipTask?.FilePath, archive.LastZipPath);

        dataProvider.ThrowOnExecute = true;
        repository.Add(NewTask("task-fail", "1"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteTaskAsync("task-fail"));
        var failed = await repository.GetByIdAsync("task-fail");
        Assert.Equal(ReportExportTaskStatus.Failed, failed?.Status);
    }

    [Fact]
    public async Task GetDownloadAsync_validates_status_expiry_and_file_existence()
    {
        var service = CreateService(out var repository, out _, out _, out _, out _);
        repository.Add(NewTask("queued", "1"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetDownloadAsync("queued"));

        var expiredPath = Path.GetTempFileName();
        repository.Add(NewTask("expired", "1", ReportExportTaskStatus.Success, expiredPath, "expired.xlsx", DateTime.Now.AddMinutes(-1)));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetDownloadAsync("expired"));

        var filePath = Path.GetTempFileName();
        repository.Add(NewTask("ok", "1", ReportExportTaskStatus.Success, filePath, "ok.xlsx", DateTime.Now.AddHours(1)));
        var download = await service.GetDownloadAsync("ok");

        Assert.Equal(filePath, download.FilePath);
        Assert.Equal("ok.xlsx", download.FileName);
        File.Delete(filePath);
        File.Delete(expiredPath);
    }

    private static ReportExportService CreateService(out InMemoryReportExportTaskRepository repository, out FakeReportExportDataProvider dataProvider, out FakeReportWorkbookWriter writer, out FakeReportArchiveService archive, out RecordingReportExportJobScheduler scheduler)
    {
        repository = new InMemoryReportExportTaskRepository();
        dataProvider = new FakeReportExportDataProvider();
        writer = new FakeReportWorkbookWriter();
        archive = new FakeReportArchiveService();
        scheduler = new RecordingReportExportJobScheduler();
        return new ReportExportService(repository, dataProvider, writer, archive, scheduler);
    }

    private static ReportExportTask NewTask(string id, string groupIds, string status = ReportExportTaskStatus.Queued, string? filePath = null, string? fileName = null, DateTime? expiredAt = null)
    {
        return new ReportExportTask
        {
            Id = id,
            GroupIds = groupIds,
            StartTime = new DateTime(2026, 6, 1),
            EndTime = new DateTime(2026, 6, 1, 23, 59, 59),
            Status = status,
            Progress = status == ReportExportTaskStatus.Success ? 100 : 0,
            Stage = "排队中",
            FilePath = filePath,
            FileName = fileName,
            CreatedAt = DateTime.Now,
            ExpiredAt = expiredAt ?? DateTime.Now.AddHours(1)
        };
    }
}

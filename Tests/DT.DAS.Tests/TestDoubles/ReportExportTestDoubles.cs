using System.Data;
using DT.DAS.Application.Reports;
using DT.DAS.Domain.Entities;
using DT.DAS.Domain.Interfaces;

namespace DT.DAS.Tests.TestDoubles;

internal sealed class InMemoryReportExportTaskRepository : IReportExportTaskRepository
{
    private readonly Dictionary<string, ReportExportTask> _tasks = new(StringComparer.OrdinalIgnoreCase);

    public bool StorageEnsured { get; private set; }
    public ReportExportTask? LastCreated { get; private set; }

    public Task EnsureStorageAsync(CancellationToken ct = default)
    {
        StorageEnsured = true;
        return Task.CompletedTask;
    }

    public Task CreateAsync(ReportExportTask task, CancellationToken ct = default)
    {
        LastCreated = Clone(task);
        _tasks[task.Id] = Clone(task);
        return Task.CompletedTask;
    }

    public Task<ReportExportTask?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return Task.FromResult(_tasks.TryGetValue(id, out var task) ? Clone(task) : null);
    }

    public Task UpdateProgressAsync(string id, string status, int progress, string stage, string? errorMessage = null, CancellationToken ct = default)
    {
        if (_tasks.TryGetValue(id, out var task))
        {
            task.Status = status;
            task.Progress = progress;
            task.Stage = stage;
            task.ErrorMessage = errorMessage;
        }

        return Task.CompletedTask;
    }

    public Task CompleteAsync(string id, string filePath, string fileName, CancellationToken ct = default)
    {
        if (_tasks.TryGetValue(id, out var task))
        {
            task.Status = ReportExportTaskStatus.Success;
            task.Progress = 100;
            task.Stage = "导出完成";
            task.FilePath = filePath;
            task.FileName = fileName;
            task.ErrorMessage = null;
        }

        return Task.CompletedTask;
    }

    public Task FailAsync(string id, string errorMessage, CancellationToken ct = default)
    {
        if (_tasks.TryGetValue(id, out var task))
        {
            task.Status = ReportExportTaskStatus.Failed;
            task.Stage = "导出失败";
            task.ErrorMessage = errorMessage;
        }

        return Task.CompletedTask;
    }

    public void Add(ReportExportTask task) => _tasks[task.Id] = Clone(task);

    private static ReportExportTask Clone(ReportExportTask task)
    {
        return new ReportExportTask
        {
            Id = task.Id,
            GroupIds = task.GroupIds,
            StartTime = task.StartTime,
            EndTime = task.EndTime,
            Status = task.Status,
            Progress = task.Progress,
            Stage = task.Stage,
            FilePath = task.FilePath,
            FileName = task.FileName,
            ErrorMessage = task.ErrorMessage,
            CreatedAt = task.CreatedAt,
            ExpiredAt = task.ExpiredAt
        };
    }
}

internal sealed class FakeReportExportDataProvider : IReportExportDataProvider
{
    public List<ReportExportGroupDefinition> Groups { get; } = new();
    public bool ThrowOnExecute { get; set; }

    public Task<IList<ReportExportGroupDefinition>> GetGroupDefinitionsAsync(IEnumerable<int> groupIds, CancellationToken ct = default)
    {
        var idSet = groupIds.ToHashSet();
        return Task.FromResult<IList<ReportExportGroupDefinition>>(Groups.Where(x => idSet.Contains(x.GroupId)).OrderBy(x => x.GroupId).ToList());
    }

    public Task<IList<ReportDataSet>> ExecuteGroupReportAsync(int groupId, string procedureName, DateTime startTime, DateTime endTime, CancellationToken ct = default)
    {
        if (ThrowOnExecute)
        {
            throw new InvalidOperationException("export failed");
        }

        var table = new DataTable();
        table.Columns.Add("Name", typeof(string));
        table.Rows.Add("G" + groupId);
        return Task.FromResult<IList<ReportDataSet>>(new[] { new ReportDataSet { SheetName = "Sheet/Name:*?ThatIsWayTooLongForExcel", Data = table } });
    }
}

internal sealed class FakeReportWorkbookWriter : IReportWorkbookWriter
{
    public List<string> WrittenFiles { get; } = new();

    public void Write(string filePath, IList<ReportDataSet> dataSets)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(filePath, "xlsx");
        WrittenFiles.Add(filePath);
    }
}

internal sealed class FakeReportArchiveService : IReportArchiveService
{
    public string? LastZipPath { get; private set; }

    public void CreateZip(string zipPath, IEnumerable<string> filePaths)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(zipPath)!);
        File.WriteAllText(zipPath, string.Join("\n", filePaths.Select(Path.GetFileName)));
        LastZipPath = zipPath;
    }
}

internal sealed class RecordingReportExportJobScheduler : IReportExportJobScheduler
{
    public string? LastTaskId { get; private set; }

    public string Enqueue(string taskId)
    {
        LastTaskId = taskId;
        return "recorded";
    }
}

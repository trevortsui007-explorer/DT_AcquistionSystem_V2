using DT.DAS.Application.Reports.Contracts;
using DT.DAS.Domain.Entities;
using DT.DAS.Domain.Interfaces;

namespace DT.DAS.Application.Reports.Services;

public sealed class ReportExportService : IReportExportService
{
    private const int MaxExportDays = 7;
    private readonly IReportExportTaskRepository _taskRepository;
    private readonly IReportExportDataProvider _dataProvider;
    private readonly IReportWorkbookWriter _workbookWriter;
    private readonly IReportArchiveService _archiveService;
    private readonly IReportExportJobScheduler _scheduler;

    public ReportExportService(
        IReportExportTaskRepository taskRepository,
        IReportExportDataProvider dataProvider,
        IReportWorkbookWriter workbookWriter,
        IReportArchiveService archiveService,
        IReportExportJobScheduler scheduler)
    {
        _taskRepository = taskRepository;
        _dataProvider = dataProvider;
        _workbookWriter = workbookWriter;
        _archiveService = archiveService;
        _scheduler = scheduler;
    }

    public async Task<ReportExportCreateResponseDto> CreateTaskAsync(ReportExportCreateRequestDto? request, CancellationToken ct = default)
    {
        ValidateRequest(request);
        await _taskRepository.EnsureStorageAsync(ct).ConfigureAwait(false);

        var groupIds = request!.GroupIds!.Distinct().OrderBy(id => id).ToList();
        var groupDefinitions = await _dataProvider.GetGroupDefinitionsAsync(groupIds, ct).ConfigureAwait(false);
        if (groupDefinitions.Count != groupIds.Count)
        {
            var foundIds = groupDefinitions.Select(x => x.GroupId).ToHashSet();
            var missingIds = groupIds.Where(id => !foundIds.Contains(id));
            throw new InvalidOperationException("未找到配置组: " + string.Join(",", missingIds));
        }

        var groupsWithoutProcedure = groupDefinitions
            .Where(x => string.IsNullOrWhiteSpace(x.ExportProcedureName))
            .Select(x => $"{x.GroupName}({x.GroupId})")
            .ToList();
        if (groupsWithoutProcedure.Count > 0)
        {
            throw new InvalidOperationException("以下配置组未配置导出存储过程: " + string.Join(", ", groupsWithoutProcedure));
        }

        var now = DateTime.Now;
        var task = new ReportExportTask
        {
            Id = Guid.NewGuid().ToString("N"),
            GroupIds = string.Join(',', groupIds),
            StartTime = request.StartTime,
            EndTime = NormalizeEndTime(request.EndTime),
            Status = ReportExportTaskStatus.Queued,
            Progress = 0,
            Stage = "排队中",
            CreatedAt = now,
            ExpiredAt = now.AddHours(24)
        };

        await _taskRepository.CreateAsync(task, ct).ConfigureAwait(false);
        try
        {
            _scheduler.Enqueue(task.Id);
        }
        catch (Exception ex)
        {
            await _taskRepository.FailAsync(task.Id, "后台任务提交失败: " + ex.Message, ct).ConfigureAwait(false);
            throw new InvalidOperationException("后台任务未成功启动，请检查 Hangfire 配置。", ex);
        }

        return new ReportExportCreateResponseDto { ExportTaskId = task.Id };
    }

    public async Task<ReportExportTaskDto?> GetTaskAsync(string id, CancellationToken ct = default)
    {
        await _taskRepository.EnsureStorageAsync(ct).ConfigureAwait(false);
        var task = await _taskRepository.GetByIdAsync(id, ct).ConfigureAwait(false);
        return task == null ? null : MapTask(task);
    }

    public async Task<ReportExportDownload> GetDownloadAsync(string id, CancellationToken ct = default)
    {
        await _taskRepository.EnsureStorageAsync(ct).ConfigureAwait(false);
        var task = await _taskRepository.GetByIdAsync(id, ct).ConfigureAwait(false);
        if (task == null)
        {
            throw new InvalidOperationException("导出任务不存在。");
        }

        if (task.Status != ReportExportTaskStatus.Success)
        {
            throw new InvalidOperationException("导出任务尚未完成。");
        }

        if (task.ExpiredAt < DateTime.Now)
        {
            throw new InvalidOperationException("导出文件已过期。");
        }

        if (string.IsNullOrWhiteSpace(task.FilePath) || !File.Exists(task.FilePath))
        {
            throw new FileNotFoundException("导出文件不存在。", task.FilePath);
        }

        var fileName = string.IsNullOrWhiteSpace(task.FileName) ? Path.GetFileName(task.FilePath) : task.FileName;
        return new ReportExportDownload
        {
            FilePath = task.FilePath,
            FileName = fileName,
            ContentType = fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
                ? "application/zip"
                : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    public async Task ExecuteTaskAsync(string id, CancellationToken ct = default)
    {
        await _taskRepository.EnsureStorageAsync(ct).ConfigureAwait(false);
        var task = await _taskRepository.GetByIdAsync(id, ct).ConfigureAwait(false);
        if (task == null)
        {
            return;
        }

        try
        {
            await _taskRepository.UpdateProgressAsync(id, ReportExportTaskStatus.Running, 5, "准备导出", ct: ct).ConfigureAwait(false);
            CleanupExpiredFiles();

            var groupIds = task.GroupIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
            var groups = await _dataProvider.GetGroupDefinitionsAsync(groupIds, ct).ConfigureAwait(false);
            var taskFolder = CreateTaskFolder(task.Id);
            var excelFiles = new List<string>();

            for (var i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                var baseProgress = 10 + (int)(70.0 * i / Math.Max(1, groups.Count));
                await _taskRepository.UpdateProgressAsync(id, ReportExportTaskStatus.Running, baseProgress, $"执行存储过程: {group.GroupName}", ct: ct).ConfigureAwait(false);

                var dataSets = await _dataProvider.ExecuteGroupReportAsync(group.GroupId, group.ExportProcedureName!, task.StartTime, task.EndTime, ct).ConfigureAwait(false);
                var excelPath = Path.Combine(taskFolder, CreateSafeFileName($"{group.GroupName}_{task.StartTime:yyyyMMdd}_{task.EndTime:yyyyMMdd}.xlsx"));

                await _taskRepository.UpdateProgressAsync(id, ReportExportTaskStatus.Running, Math.Min(90, baseProgress + 10), $"写入 Excel: {group.GroupName}", ct: ct).ConfigureAwait(false);
                _workbookWriter.Write(excelPath, dataSets);
                excelFiles.Add(excelPath);
            }

            string finalPath;
            string finalName;
            if (excelFiles.Count == 1)
            {
                finalPath = excelFiles[0];
                finalName = Path.GetFileName(finalPath);
            }
            else
            {
                await _taskRepository.UpdateProgressAsync(id, ReportExportTaskStatus.Running, 94, "压缩文件", ct: ct).ConfigureAwait(false);
                finalName = $"报表导出_{task.StartTime:yyyyMMdd}_{task.EndTime:yyyyMMdd}.zip";
                finalPath = Path.Combine(taskFolder, finalName);
                _archiveService.CreateZip(finalPath, excelFiles);
            }

            await _taskRepository.CompleteAsync(id, finalPath, finalName, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await _taskRepository.FailAsync(id, ex.Message, ct).ConfigureAwait(false);
            throw;
        }
    }

    private static void ValidateRequest(ReportExportCreateRequestDto? request)
    {
        if (request == null)
        {
            throw new InvalidOperationException("请求体不能为空。");
        }

        if (request.GroupIds == null || request.GroupIds.Count == 0)
        {
            throw new InvalidOperationException("请至少选择一个配置组。");
        }

        if (request.StartTime == default || request.EndTime == default)
        {
            throw new InvalidOperationException("开始时间和结束时间不能为空。");
        }

        var endTime = NormalizeEndTime(request.EndTime);
        if (endTime < request.StartTime)
        {
            throw new InvalidOperationException("结束时间不能早于开始时间。");
        }

        var days = (endTime.Date - request.StartTime.Date).Days + 1;
        if (days > MaxExportDays)
        {
            throw new InvalidOperationException($"单次报表导出最多支持 {MaxExportDays} 天。");
        }
    }

    private static ReportExportTaskDto MapTask(ReportExportTask task)
    {
        return new ReportExportTaskDto
        {
            Id = task.Id,
            Status = task.Status,
            Progress = task.Progress,
            Stage = task.Stage,
            FileName = task.FileName,
            ErrorMessage = task.ErrorMessage,
            CanDownload = task.Status == ReportExportTaskStatus.Success && task.ExpiredAt >= DateTime.Now && !string.IsNullOrWhiteSpace(task.FilePath) && File.Exists(task.FilePath),
            CreatedAt = task.CreatedAt,
            ExpiredAt = task.ExpiredAt
        };
    }

    private static DateTime NormalizeEndTime(DateTime endTime)
    {
        return endTime.Second == 0 && endTime.Millisecond == 0 ? endTime.AddSeconds(59) : endTime;
    }

    private static string CreateTaskFolder(string taskId)
    {
        var root = Path.Combine(AppContext.BaseDirectory, "App_Data", "ReportExports", taskId);
        Directory.CreateDirectory(root);
        return root;
    }

    private static string CreateSafeFileName(string fileName)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(c, '_');
        }

        return fileName;
    }

    private static void CleanupExpiredFiles()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "App_Data", "ReportExports");
        if (!Directory.Exists(root))
        {
            return;
        }

        foreach (var directory in Directory.GetDirectories(root))
        {
            try
            {
                if (Directory.GetCreationTime(directory).AddHours(24) < DateTime.Now)
                {
                    Directory.Delete(directory, true);
                }
            }
            catch
            {
                // Best-effort cleanup must not fail the export.
            }
        }
    }
}

using DT.DAS.Application.Configs;
using DT.DAS.Application.Tasks;
using DT.DAS.Application.Acquisition.Contracts;
using DT.DAS.Domain.Entities;

namespace DT.DAS.Application.Acquisition.Services;

public sealed class AcquisitionExecutionService : IAcquisitionExecutionService
{
    private readonly IFileConfigService _fileConfigService;
    private readonly IAcquisitionLogService _logService;
    private readonly ILogCodeGenerator _logCodeGenerator;
    private readonly IAcquisitionJobScheduler _jobScheduler;

    public AcquisitionExecutionService(
        IFileConfigService fileConfigService,
        IAcquisitionLogService logService,
        ILogCodeGenerator logCodeGenerator,
        IAcquisitionJobScheduler jobScheduler)
    {
        _fileConfigService = fileConfigService;
        _logService = logService;
        _logCodeGenerator = logCodeGenerator;
        _jobScheduler = jobScheduler;
    }

    public async Task<TaskStartResponseDto> StartByIdsAsync(string[] ids, DateTime processDate, CancellationToken ct = default)
    {
        var configs = _fileConfigService.GetByIds(ids).ToList();
        return await StartBatchAsync(configs, processDate.Date, processDate.Date, TaskTriggerTypes.Manual, "Acquisition task has been queued.", ct).ConfigureAwait(false);
    }

    public async Task<TaskStartResponseDto> StartByGroupsAsync(string[] groupIds, DateTime processDate, CancellationToken ct = default)
    {
        var configs = _fileConfigService.GetConfigsByGroupIds(groupIds).ToList();
        return await StartBatchAsync(configs, processDate.Date, processDate.Date, TaskTriggerTypes.Manual, "Grouped acquisition task has been queued.", ct).ConfigureAwait(false);
    }

    public async Task<TaskStartResponseDto> StartByTasksAsync(string[] taskIds, DateTime processDate, CancellationToken ct = default)
    {
        var configs = _fileConfigService.GetConfigsByTaskIds(taskIds).ToList();
        return await StartBatchAsync(configs, processDate.Date, processDate.Date, TaskTriggerTypes.Manual, "Scheduled acquisition task has been queued.", ct).ConfigureAwait(false);
    }

    public async Task<TaskStatusDto?> GetTaskStatusAsync(string taskLogId, CancellationToken ct = default)
    {
        var taskLog = await _logService.GetTaskLogByIdAsync(taskLogId, ct).ConfigureAwait(false);
        return taskLog == null
            ? null
            : new TaskStatusDto
            {
                TaskLogId = taskLog.Id,
                TaskCode = taskLog.TaskCode,
                TriggerType = taskLog.TriggerType,
                Status = taskLog.Status,
                TotalConfigs = taskLog.TotalConfigs,
                SuccessCount = taskLog.SuccessCount,
                FailureCount = taskLog.FailureCount,
                ProcessedCount = taskLog.ProcessedCount,
                Progress = taskLog.Progress,
                StartTime = taskLog.StartTime,
                EndTime = taskLog.EndTime,
                Message = taskLog.Message
            };
    }

    public async Task<List<TaskDetailLogDto>> GetTaskDetailsAsync(string taskLogId, CancellationToken ct = default)
    {
        var logs = await _logService.GetLogsByTaskLogIdAsync(taskLogId, ct).ConfigureAwait(false);
        return logs.Select(x => new TaskDetailLogDto
        {
            Id = x.Id,
            TaskLogId = x.TaskLogId,
            ConfigId = x.ConfigId,
            FileName = x.FileName,
            StartRow = x.StartRow,
            ProcessedRows = x.ProcessedRows,
            StartTime = x.StartTime,
            EndTime = x.EndTime,
            Status = x.Status,
            ErrorMessage = x.ErrorMessage
        }).ToList();
    }

    private async Task<TaskStartResponseDto> StartBatchAsync(List<AcquisitionConfig> configs, DateTime startDate, DateTime endDate, string triggerType, string successMessage, CancellationToken ct)
    {
        if (configs.Count == 0)
        {
            return new TaskStartResponseDto
            {
                Status = "NoData",
                Message = "No executable acquisition configs were found."
            };
        }

        var totalCount = configs.Count * ((endDate.Date - startDate.Date).Days + 1);
        var taskLogId = await _logService.RecordTaskLogEntryAsync(new AcquisitionTaskLogEntry
        {
            TaskId = 0,
            TaskCode = await _logCodeGenerator.GenerateTaskCodeAsync(triggerType, ct).ConfigureAwait(false),
            TriggerType = triggerType,
            StartTime = DateTime.Now,
            Status = "Running",
            TotalConfigs = totalCount,
            Message = "Task created and waiting for execution."
        }, ct).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(taskLogId))
        {
            throw new InvalidOperationException("Failed to create acquisition task log.");
        }

        var updateSource = ResolveUpdateSource(triggerType, startDate, endDate);
        var sealOnSuccess = updateSource == FileStateUpdateSources.ScheduledD1Backfill;
        _jobScheduler.Enqueue(taskLogId, configs.Select(x => x.Id).ToArray(), startDate, endDate, updateSource, sealOnSuccess);

        return new TaskStartResponseDto
        {
            TaskLogId = taskLogId,
            Status = "Running",
            Message = successMessage
        };
    }

    private static string ResolveUpdateSource(string triggerType, DateTime startDate, DateTime endDate)
    {
        var isHistory = endDate.Date < DateTime.Today;
        return string.Equals(triggerType, TaskTriggerTypes.Scheduled, StringComparison.Ordinal)
            ? isHistory ? FileStateUpdateSources.ScheduledD1Backfill : FileStateUpdateSources.ScheduledCurrent
            : isHistory ? FileStateUpdateSources.ManualRepair : FileStateUpdateSources.ManualCurrent;
    }
}




using DT.DAS.Application.Tasks;
using DT.DAS.Domain.Entities;
using DT.DAS.WebApi.Modules.Shared;
using Microsoft.AspNetCore.Mvc;

namespace DT.DAS.WebApi.Modules.Logs;

/// <summary>
/// Provides acquisition status, task log, and detail log APIs.
/// </summary>
[ApiController]
[Route("api/data-acquisition")]
public sealed class AcquisitionLogController : ControllerBase
{
    private readonly IAcquisitionLogService _logService;

    public AcquisitionLogController(IAcquisitionLogService logService)
    {
        _logService = logService;
    }

    /// <summary>
    /// Gets the next start row for resumable acquisition of one file config.
    /// </summary>
    [HttpGet("next-row/{configId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNextStartRow([FromRoute] int configId, [FromQuery] string? fileName, CancellationToken ct)
    {
        if (configId <= 0)
        {
            return LegacyApiResponse.Fail(this, "参数错误：ConfigId 必须大于 0");
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            return LegacyApiResponse.Fail(this, "请输入要查询的文件名");
        }

        try
        {
            var nextStartRow = await _logService.GetNextStartRowAsync(configId, fileName, ct).ConfigureAwait(false);
            return LegacyApiResponse.Success(this, "查询成功", new { configId, nextStartRow });
        }
        catch (InvalidOperationException ex)
        {
            return LegacyApiResponse.Fail(this, ex.Message);
        }
        catch (Exception ex)
        {
            return LegacyApiResponse.Fail(this, $"获取起始行失败: {ex.Message}");
        }
    }

    /// <summary>
    /// Records one acquisition detail log entry.
    /// </summary>
    [HttpPost("log")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RecordLogEntry([FromBody] AcquisitionLogEntry? entry, CancellationToken ct)
    {
        if (entry == null)
        {
            return LegacyApiResponse.Fail(this, "数据解析失败：请求体为空或格式错误");
        }

        try
        {
            var id = await _logService.RecordLogEntryAsync(entry, ct).ConfigureAwait(false);
            return LegacyApiResponse.Success(this, "明细进度记录成功", new { id });
        }
        catch (InvalidOperationException ex)
        {
            return LegacyApiResponse.Fail(this, ex.Message);
        }
        catch (Exception ex)
        {
            return LegacyApiResponse.Fail(this, $"记录明细进度异常: {ex.Message}");
        }
    }

    /// <summary>
    /// Records one acquisition task log entry.
    /// </summary>
    [HttpPost("task-log")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RecordTaskLogEntry([FromBody] AcquisitionTaskLogEntry? entry, CancellationToken ct)
    {
        if (entry == null)
        {
            return LegacyApiResponse.Fail(this, "数据解析失败：请求体为空或格式错误");
        }

        try
        {
            var id = await _logService.RecordTaskLogEntryAsync(entry, ct).ConfigureAwait(false);
            return LegacyApiResponse.Success(this, "任务日志记录成功", new { id });
        }
        catch (InvalidOperationException ex)
        {
            return LegacyApiResponse.Fail(this, ex.Message);
        }
        catch (Exception ex)
        {
            return LegacyApiResponse.Fail(this, $"记录任务日志异常: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates one acquisition task log status using legacy status and success count semantics.
    /// </summary>
    [HttpPut("task-log/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateTaskStatus([FromRoute] string id, [FromBody] AcquisitionTaskLogEntry? request, CancellationToken ct)
    {
        if (request == null)
        {
            return LegacyApiResponse.Fail(this, "数据解析失败：请求体为空或格式错误");
        }

        try
        {
            var success = await _logService.UpdateTaskStatusAsync(id, request.Status, request.SuccessCount, ct).ConfigureAwait(false);
            return success ? LegacyApiResponse.Success(this, "任务状态更新成功") : LegacyApiResponse.Fail(this, "更新失败，未找到对应记录");
        }
        catch (Exception ex)
        {
            return LegacyApiResponse.Fail(this, $"更新异常: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets one acquisition task log by id.
    /// </summary>
    [HttpGet("task-log/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTaskLog([FromRoute] string id, CancellationToken ct)
    {
        var taskLog = await _logService.GetTaskLogByIdAsync(id, ct).ConfigureAwait(false);
        return taskLog != null ? LegacyApiResponse.Success(this, "查询成功", taskLog) : LegacyApiResponse.Fail(this, "未找到任务日志");
    }

    /// <summary>
    /// Gets detail logs under one acquisition task log.
    /// </summary>
    [HttpGet("task-log/{id}/logs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDetailLogs([FromRoute] string id, CancellationToken ct)
    {
        var logs = await _logService.GetLogsByTaskLogIdAsync(id, ct).ConfigureAwait(false);
        return LegacyApiResponse.Success(this, "查询成功", logs);
    }

    /// <summary>
    /// Gets acquisition task logs by page and optional filters.
    /// </summary>
    [HttpGet("task-logs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTaskLogs(
        [FromQuery] int pageNo = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? startTime = null,
        [FromQuery] DateTime? endTime = null,
        [FromQuery] int? taskId = null,
        CancellationToken ct = default)
    {
        var rowsTask = _logService.GetTaskLogsAsync(pageNo, pageSize, status, startTime, endTime, taskId, ct);
        var countTask = _logService.GetTaskLogsCountAsync(status, startTime, endTime, taskId, ct);
        await Task.WhenAll(rowsTask, countTask).ConfigureAwait(false);
        return LegacyApiResponse.Page(this, "查询成功", countTask.Result, rowsTask.Result);
    }
}

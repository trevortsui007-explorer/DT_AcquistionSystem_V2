using DT.DAS.Application.Acquisition;
using DT.DAS.Application.Acquisition.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace DT.DAS.WebApi.Modules.Acquisition;

/// <summary>
/// Provides acquisition task startup and execution status APIs.
/// </summary>
[ApiController]
[Route("api/data-acquisition/execution")]
public sealed class AcquisitionExecutionController : ControllerBase
{
    private readonly IAcquisitionExecutionService _executionService;

    public AcquisitionExecutionController(IAcquisitionExecutionService executionService)
    {
        _executionService = executionService;
    }

    /// <summary>
    /// Starts an acquisition task for one or more acquisition config IDs.
    /// </summary>
    /// <param name="ids">Acquisition config IDs.</param>
    /// <param name="processDate">Business date to process. Defaults to current local date and time.</param>
    /// <param name="ct">Request cancellation token.</param>
    /// <returns>Queued task metadata including the task log ID.</returns>
    [HttpPost("start/by-ids")]
    [ProducesResponseType(typeof(TaskStartResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StartByIds([FromQuery] string[] ids, [FromQuery] DateTime? processDate, CancellationToken ct)
    {
        if (ids.Length == 0)
        {
            return BadRequest(new { message = "ids is required." });
        }

        var result = await _executionService.StartByIdsAsync(ids, processDate ?? DateTime.Now, ct).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Starts an acquisition task for one or more acquisition group IDs.
    /// </summary>
    /// <param name="groupIds">Acquisition group IDs.</param>
    /// <param name="processDate">Business date to process. Defaults to current local date and time.</param>
    /// <param name="ct">Request cancellation token.</param>
    /// <returns>Queued task metadata including the task log ID.</returns>
    [HttpPost("start/by-groups")]
    [ProducesResponseType(typeof(TaskStartResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StartByGroups([FromQuery] string[] groupIds, [FromQuery] DateTime? processDate, CancellationToken ct)
    {
        if (groupIds.Length == 0)
        {
            return BadRequest(new { message = "groupIds is required." });
        }

        var result = await _executionService.StartByGroupsAsync(groupIds, processDate ?? DateTime.Now, ct).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Starts an acquisition task for one or more scheduled task IDs.
    /// </summary>
    /// <param name="taskIds">Scheduled task IDs.</param>
    /// <param name="processDate">Business date to process. Defaults to current local date and time.</param>
    /// <param name="ct">Request cancellation token.</param>
    /// <returns>Queued task metadata including the task log ID.</returns>
    [HttpPost("start/by-tasks")]
    [ProducesResponseType(typeof(TaskStartResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StartByTasks([FromQuery] string[] taskIds, [FromQuery] DateTime? processDate, CancellationToken ct)
    {
        if (taskIds.Length == 0)
        {
            return BadRequest(new { message = "taskIds is required." });
        }

        var result = await _executionService.StartByTasksAsync(taskIds, processDate ?? DateTime.Now, ct).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Gets the current status for an acquisition task log.
    /// </summary>
    /// <param name="taskLogId">Task log identifier returned by a start endpoint.</param>
    /// <param name="ct">Request cancellation token.</param>
    /// <returns>Current task status and progress.</returns>
    [HttpGet("{taskLogId}/status")]
    [ProducesResponseType(typeof(TaskStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTaskStatus([FromRoute] string taskLogId, CancellationToken ct)
    {
        var result = await _executionService.GetTaskStatusAsync(taskLogId, ct).ConfigureAwait(false);
        return result == null ? NotFound(new { message = $"Task log '{taskLogId}' was not found." }) : Ok(result);
    }

    /// <summary>
    /// Gets detail log entries for an acquisition task log.
    /// </summary>
    /// <param name="taskLogId">Task log identifier returned by a start endpoint.</param>
    /// <param name="ct">Request cancellation token.</param>
    /// <returns>File-level execution log details.</returns>
    [HttpGet("{taskLogId}/details")]
    [ProducesResponseType(typeof(List<TaskDetailLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTaskDetails([FromRoute] string taskLogId, CancellationToken ct)
    {
        var result = await _executionService.GetTaskDetailsAsync(taskLogId, ct).ConfigureAwait(false);
        return Ok(result);
    }
}

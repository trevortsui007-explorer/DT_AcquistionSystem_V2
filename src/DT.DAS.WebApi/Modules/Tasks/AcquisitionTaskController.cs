using DT.DAS.Application.Tasks;
using DT.DAS.Domain.Entities;
using DT.DAS.WebApi.Modules.Configs;
using DT.DAS.WebApi.Modules.Shared;
using Microsoft.AspNetCore.Mvc;

namespace DT.DAS.WebApi.Modules.Tasks;

/// <summary>
/// Provides acquisition task management APIs.
/// </summary>
[ApiController]
[Route("api/data-acquisition/tasks")]
public sealed class AcquisitionTaskController : ControllerBase
{
    private readonly IAcquisitionTaskService _taskService;

    public AcquisitionTaskController(IAcquisitionTaskService taskService)
    {
        _taskService = taskService;
    }

    /// <summary>
    /// Gets all acquisition tasks with associated groups.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetTaskList([FromQuery] string? tableName = null, [FromQuery] string? databaseName = null)
    {
        return LegacyApiResponse.Success(this, "查询成功", _taskService.GetList(tableName, databaseName));
    }

    /// <summary>
    /// Gets one acquisition task with associated groups.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetTaskEntity([FromRoute] int id, [FromQuery] string? tableName = null, [FromQuery] string? databaseName = null)
    {
        if (id <= 0)
        {
            return LegacyApiResponse.Fail(this, "参数错误");
        }

        var entity = _taskService.GetById(id.ToString(), tableName, databaseName);
        return entity != null ? LegacyApiResponse.Success(this, "查询成功", entity) : LegacyApiResponse.Fail(this, "未找到任务数据");
    }

    /// <summary>
    /// Gets acquisition tasks by task mode.
    /// </summary>
    [HttpGet("mode/{mode:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetTasksByMode([FromRoute] int mode, [FromQuery] string? tableName = null, [FromQuery] string? databaseName = null)
    {
        return LegacyApiResponse.Success(this, "查询成功", _taskService.GetByMode(mode, tableName, databaseName));
    }

    /// <summary>
    /// Creates an acquisition task.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult CreateTask([FromBody] AcquisitionTask? task, [FromQuery] string? tableName = null, [FromQuery] string? databaseName = null)
    {
        if (task == null)
        {
            return LegacyApiResponse.Fail(this, "数据解析失败");
        }

        var id = _taskService.CreateTask(task, tableName, databaseName);
        return id > 0 ? LegacyApiResponse.Success(this, "任务创建成功", id) : LegacyApiResponse.Fail(this, "任务创建失败");
    }

    /// <summary>
    /// Updates an acquisition task.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult UpdateTask([FromRoute] int id, [FromBody] AcquisitionTask? task, [FromQuery] string? tableName = null, [FromQuery] string? databaseName = null)
    {
        if (task == null)
        {
            return LegacyApiResponse.Fail(this, "数据解析失败");
        }

        task.Id = id;
        var success = _taskService.UpdateTask(task, tableName, databaseName);
        return success ? LegacyApiResponse.Success(this, "任务更新成功") : LegacyApiResponse.Fail(this, "任务更新失败");
    }

    /// <summary>
    /// Deletes one or more acquisition tasks.
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult DeleteTasks([FromQuery] string[]? ids = null, [FromQuery] string? tableName = null, [FromQuery] string? databaseName = null)
    {
        var idArray = ConfigQueryHelpers.SplitValues(ids);
        if (idArray.Length == 0)
        {
            return LegacyApiResponse.Fail(this, "缺少待删除的任务 ids");
        }

        var success = _taskService.DeleteTasks(idArray, tableName, databaseName);
        return success ? LegacyApiResponse.Success(this, $"成功删除 {idArray.Length} 个任务") : LegacyApiResponse.Fail(this, "任务删除失败");
    }

    /// <summary>
    /// Enables or disables one or more acquisition tasks.
    /// </summary>
    [HttpPatch("status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult SetEnabledStatus([FromQuery] string[]? ids = null, [FromQuery] bool isEnabled = false, [FromQuery] string? tableName = null, [FromQuery] string? databaseName = null)
    {
        var idArray = ConfigQueryHelpers.SplitValues(ids);
        if (idArray.Length == 0)
        {
            return LegacyApiResponse.Fail(this, "缺少参数 ids");
        }

        var success = _taskService.SetEnabledStatus(idArray, isEnabled, tableName, databaseName);
        return success ? LegacyApiResponse.Success(this, "任务状态更新成功") : LegacyApiResponse.Fail(this, "任务状态更新失败");
    }

    /// <summary>
    /// Gets group IDs associated with an acquisition task.
    /// </summary>
    [HttpGet("{taskId:int}/groups")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetAssociatedGroupIds([FromRoute] int taskId, [FromQuery] string? linkTableName = null, [FromQuery] string? databaseName = null)
    {
        return LegacyApiResponse.Success(this, "查询成功", _taskService.GetAssociatedGroupIds(taskId, linkTableName, databaseName));
    }

    /// <summary>
    /// Replaces all groups associated with an acquisition task.
    /// </summary>
    [HttpPost("{taskId:int}/groups")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult AssignGroupsToTask([FromRoute] int taskId, [FromQuery] string[]? ids = null, [FromQuery] string? linkTableName = null, [FromQuery] string? databaseName = null)
    {
        var groupIds = ConfigQueryHelpers.SplitIntValues(ids);
        var success = _taskService.AssignGroupsToTask(taskId, groupIds, linkTableName, databaseName);
        return success ? LegacyApiResponse.Success(this, "组关联分配成功") : LegacyApiResponse.Fail(this, "组关联分配失败");
    }
}

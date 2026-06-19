using DT.DAS.Application.Configs;
using DT.DAS.Domain.Entities;
using DT.DAS.WebApi.Modules.Shared;
using Microsoft.AspNetCore.Mvc;

namespace DT.DAS.WebApi.Modules.Configs;

/// <summary>
/// Provides acquisition file configuration group management APIs.
/// </summary>
[ApiController]
[Route("api/data-acquisition/file-configs/group")]
public sealed class FileConfigGroupController : ControllerBase
{
    private readonly IFileConfigGroupService _fileConfigGroupService;

    public FileConfigGroupController(IFileConfigGroupService fileConfigGroupService)
    {
        _fileConfigGroupService = fileConfigGroupService;
    }

    /// <summary>
    /// Gets all configuration groups.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetConfigGroupList([FromQuery] string? tableName = null, [FromQuery] string? linkTableName = null, [FromQuery] string? databaseName = null)
    {
        var data = _fileConfigGroupService.GetList(tableName, linkTableName, databaseName);
        return LegacyApiResponse.Success(this, "查询成功", data);
    }

    /// <summary>
    /// Gets a configuration group by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetConfigGroupEntity([FromRoute] int id, [FromQuery] string? tableName = null, [FromQuery] string? databaseName = null)
    {
        if (id <= 0)
        {
            return LegacyApiResponse.Fail(this, "参数错误");
        }

        var entity = _fileConfigGroupService.GetByIds(new[] { id.ToString() }, tableName, databaseName).FirstOrDefault();
        return entity != null
            ? LegacyApiResponse.Success(this, "查询成功", entity)
            : LegacyApiResponse.Fail(this, "未找到数据");
    }

    /// <summary>
    /// Creates a configuration group.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult CreateConfigGroup([FromBody] AcquisitionGroup? group, [FromQuery] string? tableName = null, [FromQuery] string? databaseName = null)
    {
        if (group == null)
        {
            return LegacyApiResponse.Fail(this, "数据解析失败");
        }

        var newId = _fileConfigGroupService.CreateConfigGroup(group, tableName, databaseName);
        return newId > 0
            ? LegacyApiResponse.Success(this, "新增成功", newId)
            : LegacyApiResponse.Fail(this, "新增失败");
    }

    /// <summary>
    /// Updates a configuration group.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult UpdateConfigGroup([FromRoute] int id, [FromBody] AcquisitionGroup? group, [FromQuery] string? tableName = null, [FromQuery] string? databaseName = null)
    {
        if (group == null)
        {
            return LegacyApiResponse.Fail(this, "数据解析失败");
        }

        group.Id = id;
        var success = _fileConfigGroupService.UpdateConfig(group, tableName, databaseName);
        return success
            ? LegacyApiResponse.Success(this, "更新成功")
            : LegacyApiResponse.Fail(this, "更新失败");
    }

    /// <summary>
    /// Deletes one or more configuration groups.
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult DeleteConfigGroups([FromQuery] string[]? ids = null, [FromQuery] string? tableName = null, [FromQuery] string? databaseName = null)
    {
        var idArray = ConfigQueryHelpers.SplitValues(ids);
        if (idArray.Length == 0)
        {
            return LegacyApiResponse.Fail(this, "缺少待删除的 ids");
        }

        var success = _fileConfigGroupService.DeleteConfigs(idArray, tableName, databaseName);
        return success
            ? LegacyApiResponse.Success(this, $"成功删除 {idArray.Length} 条记录")
            : LegacyApiResponse.Fail(this, "删除失败");
    }

    /// <summary>
    /// Gets enabled states for one or more configuration groups.
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetConfigGroupStatus([FromQuery] string[]? ids = null, [FromQuery] string? tableName = null, [FromQuery] string? databaseName = null)
    {
        var idArray = ConfigQueryHelpers.SplitValues(ids);
        if (idArray.Length == 0)
        {
            return LegacyApiResponse.Fail(this, "缺少参数 ids");
        }

        var data = _fileConfigGroupService.GetStatusByIds(idArray, "GroupName", tableName, databaseName);
        return LegacyApiResponse.Success(this, "查询成功", data);
    }

    /// <summary>
    /// Enables or disables one or more configuration groups.
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

        var success = _fileConfigGroupService.SetEnabledStatus(idArray, isEnabled, tableName, databaseName);
        return success
            ? LegacyApiResponse.Success(this, "状态更新成功")
            : LegacyApiResponse.Fail(this, "状态更新失败");
    }

    /// <summary>
    /// Adds one or more configurations to a configuration group.
    /// </summary>
    [HttpPost("{groupId:int}/configs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> AddConfigsToGroup([FromRoute] int groupId, [FromQuery] string[]? ids = null, [FromQuery] string? linkTableName = null, [FromQuery] string? databaseName = null, CancellationToken ct = default)
    {
        var configIds = ConfigQueryHelpers.SplitIntValues(ids);
        if (configIds.Length == 0)
        {
            return LegacyApiResponse.Fail(this, "缺少待关联的配置 ID 列表");
        }

        var success = await _fileConfigGroupService.AddConfigsToGroupAsync(groupId, configIds, linkTableName, databaseName, ct).ConfigureAwait(false);
        return success
            ? LegacyApiResponse.Success(this, "关联成功")
            : LegacyApiResponse.Fail(this, "关联失败");
    }

    /// <summary>
    /// Removes one or more configurations from a configuration group.
    /// </summary>
    [HttpDelete("{groupId:int}/configs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult RemoveConfigsFromGroup([FromRoute] int groupId, [FromQuery] string[]? ids = null, [FromQuery] string? linkTableName = null, [FromQuery] string? databaseName = null)
    {
        var configIds = ConfigQueryHelpers.SplitIntValues(ids);
        if (configIds.Length == 0)
        {
            return LegacyApiResponse.Fail(this, "缺少待解除的配置 ID 列表");
        }

        var success = _fileConfigGroupService.RemoveConfigsFromGroup(groupId, configIds, linkTableName, databaseName);
        return success
            ? LegacyApiResponse.Success(this, "解除关联成功")
            : LegacyApiResponse.Fail(this, "解除关联失败");
    }
}

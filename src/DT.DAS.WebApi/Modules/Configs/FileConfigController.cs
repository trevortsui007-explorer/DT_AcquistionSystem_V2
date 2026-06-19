using DT.DAS.Application.Configs;
using DT.DAS.Application.Configs.Contracts;
using DT.DAS.Domain.Entities;
using DT.DAS.WebApi.Modules.Shared;
using Microsoft.AspNetCore.Mvc;

namespace DT.DAS.WebApi.Modules.Configs;

/// <summary>
/// Provides acquisition file configuration management APIs.
/// </summary>
[ApiController]
[Route("api/data-acquisition/file-configs")]
public sealed class FileConfigController : ControllerBase
{
    private readonly IFileConfigService _fileConfigService;

    public FileConfigController(IFileConfigService fileConfigService)
    {
        _fileConfigService = fileConfigService;
    }

    /// <summary>
    /// Gets file acquisition configurations by paging or as a full list.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetConfigList(
        [FromQuery] bool all = false,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10,
        [FromQuery] string[]? ids = null,
        [FromQuery] string[]? groupIds = null,
        [FromQuery] string[]? taskIds = null,
        [FromQuery] string? tableName = null,
        [FromQuery] string? databaseName = null,
        [FromQuery] string? linkTableName = null)
    {
        var options = new FileConfigQueryOptions
        {
            TableName = tableName,
            DatabaseName = databaseName,
            LinkTableName = linkTableName,
            Ids = ConfigQueryHelpers.SplitValues(ids),
            GroupIds = ConfigQueryHelpers.SplitValues(groupIds),
            TaskIds = ConfigQueryHelpers.SplitValues(taskIds)
        };

        if (all)
        {
            var data = _fileConfigService.GetFileConfigs(options);
            return LegacyApiResponse.Success(this, "查询成功", data);
        }

        var pagedData = _fileConfigService.GetFileConfigsPaged(options, page, limit);
        return LegacyApiResponse.Page(this, "查询成功", pagedData.Total, pagedData.List);
    }

    /// <summary>
    /// Gets one file acquisition configuration by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetConfigEntity([FromRoute] int id, [FromQuery] string? tableName = null, [FromQuery] string? databaseName = null)
    {
        if (id <= 0)
        {
            return LegacyApiResponse.Fail(this, "参数错误：ID不能为空");
        }

        var entity = _fileConfigService.GetByIds(new[] { id.ToString() }, tableName, databaseName).FirstOrDefault();
        return entity != null
            ? LegacyApiResponse.Success(this, "查询成功", entity)
            : LegacyApiResponse.Fail(this, "未找到该配置信息");
    }

    /// <summary>
    /// Creates a file acquisition configuration.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult CreateConfig([FromBody] AcquisitionConfig? config, [FromQuery] string? tableName = null, [FromQuery] string? databaseName = null)
    {
        if (config == null)
        {
            return LegacyApiResponse.Fail(this, "数据解析失败");
        }

        var newId = _fileConfigService.CreateConfig(config, tableName, databaseName);
        return newId > 0
            ? LegacyApiResponse.Success(this, "新增成功", newId)
            : LegacyApiResponse.Fail(this, "新增失败");
    }

    /// <summary>
    /// Updates a file acquisition configuration.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult UpdateConfig([FromRoute] int id, [FromBody] AcquisitionConfig? config, [FromQuery] string? tableName = null, [FromQuery] string? databaseName = null)
    {
        if (config == null)
        {
            return LegacyApiResponse.Fail(this, "数据解析失败");
        }

        config.Id = id;
        var success = _fileConfigService.UpdateConfig(config, tableName, databaseName);
        return success
            ? LegacyApiResponse.Success(this, "更新成功")
            : LegacyApiResponse.Fail(this, "更新失败");
    }

    /// <summary>
    /// Deletes one or more file acquisition configurations.
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult DeleteConfigs([FromQuery] string[]? ids = null, [FromQuery] string? tableName = null, [FromQuery] string? databaseName = null)
    {
        var idArray = ConfigQueryHelpers.SplitValues(ids);
        if (idArray.Length == 0)
        {
            return LegacyApiResponse.Fail(this, "缺少待删除的 ids 参数");
        }

        var success = _fileConfigService.DeleteConfigs(idArray, tableName, databaseName);
        return success
            ? LegacyApiResponse.Success(this, $"成功删除 {idArray.Length} 条记录")
            : LegacyApiResponse.Fail(this, "删除失败");
    }

    /// <summary>
    /// Gets enabled states for one or more file acquisition configurations.
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetConfigStatus([FromQuery] string[]? ids = null, [FromQuery] string? tableName = null, [FromQuery] string? databaseName = null)
    {
        var idArray = ConfigQueryHelpers.SplitValues(ids);
        if (idArray.Length == 0)
        {
            return LegacyApiResponse.Fail(this, "缺少参数 ids");
        }

        var data = _fileConfigService.GetStatusByIds(idArray, "EqName", tableName, databaseName);
        return LegacyApiResponse.Success(this, "查询成功", data);
    }

    /// <summary>
    /// Enables or disables one or more file acquisition configurations.
    /// </summary>
    [HttpPatch("status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult SetConfigStatus([FromQuery] string[]? ids = null, [FromQuery] bool isEnabled = false, [FromQuery] string? tableName = null, [FromQuery] string? databaseName = null)
    {
        var idArray = ConfigQueryHelpers.SplitValues(ids);
        if (idArray.Length == 0)
        {
            return LegacyApiResponse.Fail(this, "参数错误：未指定ID");
        }

        var success = _fileConfigService.SetEnabledStatus(idArray, isEnabled, tableName, databaseName);
        return success
            ? LegacyApiResponse.Success(this, "操作成功")
            : LegacyApiResponse.Fail(this, "操作失败");
    }
}

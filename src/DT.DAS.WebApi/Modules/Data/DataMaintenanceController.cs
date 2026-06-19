using DT.DAS.Application.Data;
using DT.DAS.Application.Data.Contracts;
using DT.DAS.WebApi.Modules.Shared;
using Microsoft.AspNetCore.Mvc;

namespace DT.DAS.WebApi.Modules.Data;

/// <summary>
/// Provides table maintenance, bulk import, and post-processing APIs.
/// </summary>
[ApiController]
[Route("api/data-acquisition")]
public sealed class DataMaintenanceController : ControllerBase
{
    private readonly IDataMaintenanceService _service;

    public DataMaintenanceController(IDataMaintenanceService service)
    {
        _service = service;
    }

    /// <summary>
    /// Imports rows into a destination table with SqlBulkCopy.
    /// </summary>
    [HttpPost("bulk-import")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkImport([FromBody] BulkImportRequest? request, CancellationToken ct)
    {
        if (request == null)
        {
            return LegacyApiResponse.Fail(this, "请求参数错误，TableName 不能为空");
        }

        try
        {
            var result = await _service.BulkImportAsync(request, ct).ConfigureAwait(false);
            return LegacyApiResponse.Success(this, "批量导入成功", result);
        }
        catch (Exception ex)
        {
            return LegacyApiResponse.Fail(this, $"导入发生致命错误: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes a post-processing stored procedure.
    /// </summary>
    [HttpPost("execute-post-process")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExecutePostProcess([FromQuery] string? flag = null, [FromQuery] string? sproc = null, [FromBody] PostProcessRequest? request = null, CancellationToken ct = default)
    {
        var effectiveRequest = new PostProcessRequest
        {
            Flag = request?.Flag ?? flag,
            Sproc = request?.Sproc ?? sproc
        };

        try
        {
            await _service.ExecutePostProcessAsync(effectiveRequest, ct).ConfigureAwait(false);
            return LegacyApiResponse.Success(this, "存储过程执行成功");
        }
        catch (Exception ex)
        {
            return LegacyApiResponse.Fail(this, $"存储过程执行失败: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a table when it does not already exist.
    /// </summary>
    [HttpPost("create-table")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateTable([FromBody] CreateTableRequest? request, CancellationToken ct)
    {
        if (request == null)
        {
            return LegacyApiResponse.Fail(this, "表名不能为空");
        }

        try
        {
            var result = await _service.CreateTableAsync(request, ct).ConfigureAwait(false);
            return LegacyApiResponse.Success(this, $"表 [{request.TableName}] 验证/创建成功", result);
        }
        catch (Exception ex)
        {
            return LegacyApiResponse.Fail(this, $"建表发生异常: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets table field metadata.
    /// </summary>
    [HttpGet("fields/{tableName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTableFields([FromRoute] string tableName, CancellationToken ct)
    {
        try
        {
            var fields = await _service.GetTableFieldsAsync(tableName, ct).ConfigureAwait(false);
            return LegacyApiResponse.Success(this, "获取成功", fields);
        }
        catch (Exception ex)
        {
            return LegacyApiResponse.Fail(this, $"获取表字段失败: {ex.Message}");
        }
    }
}

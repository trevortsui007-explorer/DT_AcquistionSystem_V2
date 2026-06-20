using DT.DAS.Application.Reports;
using DT.DAS.Application.Reports.Contracts;
using DT.DAS.WebApi.Modules.Shared;
using Microsoft.AspNetCore.Mvc;

namespace DT.DAS.WebApi.Modules.Reports;

/// <summary>
/// Provides report export task APIs.
/// </summary>
[ApiController]
[Route("api/data-acquisition/export")]
public sealed class ReportExportController : ControllerBase
{
    private readonly IReportExportService _reportExportService;

    public ReportExportController(IReportExportService reportExportService)
    {
        _reportExportService = reportExportService;
    }

    /// <summary>
    /// Creates a report export task.
    /// </summary>
    [HttpPost("tasks")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateTask([FromBody] ReportExportCreateRequestDto? request, CancellationToken ct)
    {
        try
        {
            var result = await _reportExportService.CreateTaskAsync(request, ct).ConfigureAwait(false);
            return LegacyApiResponse.Success(this, "报表导出任务已创建", result);
        }
        catch (Exception ex)
        {
            return LegacyApiResponse.Fail(this, ex.Message);
        }
    }

    /// <summary>
    /// Gets a report export task by id.
    /// </summary>
    [HttpGet("tasks/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTask([FromRoute] string id, CancellationToken ct)
    {
        try
        {
            var task = await _reportExportService.GetTaskAsync(id, ct).ConfigureAwait(false);
            return task == null
                ? LegacyApiResponse.Fail(this, "导出任务不存在")
                : LegacyApiResponse.Success(this, "获取导出任务成功", task);
        }
        catch (Exception ex)
        {
            return LegacyApiResponse.Fail(this, ex.Message);
        }
    }

    /// <summary>
    /// Downloads a completed report export file.
    /// </summary>
    [HttpGet("tasks/{id}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadTaskFile([FromRoute] string id, CancellationToken ct)
    {
        try
        {
            var download = await _reportExportService.GetDownloadAsync(id, ct).ConfigureAwait(false);
            var stream = new FileStream(download.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return File(stream, download.ContentType, download.FileName);
        }
        catch (Exception ex)
        {
            return LegacyApiResponse.Fail(this, ex.Message);
        }
    }
}

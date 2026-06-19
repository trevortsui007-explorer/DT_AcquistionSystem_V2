using DT.DAS.Application.Configs;
using DT.DAS.Application.Files;
using DT.DAS.Application.Tasks;
using DT.DAS.WebApi.Modules.Shared;
using Microsoft.AspNetCore.Mvc;

namespace DT.DAS.WebApi.Modules.Files;

/// <summary>
/// Provides file access, preview, discovery, and acquisition file state APIs.
/// </summary>
[ApiController]
[Route("api/data-acquisition/files")]
public sealed class FileProviderController : ControllerBase
{
    private readonly IFileAccessService _fileAccessService;
    private readonly IDataPreviewService _previewService;
    private readonly IFileDiscoveryService _discoveryService;
    private readonly IFileConfigService _configService;
    private readonly IAcquisitionFileStateService _fileStateService;

    public FileProviderController(
        IFileAccessService fileAccessService,
        IDataPreviewService previewService,
        IFileDiscoveryService discoveryService,
        IFileConfigService configService,
        IAcquisitionFileStateService fileStateService)
    {
        _fileAccessService = fileAccessService;
        _previewService = previewService;
        _discoveryService = discoveryService;
        _configService = configService;
        _fileStateService = fileStateService;
    }

    /// <summary>
    /// Gets file names from a local or FTP directory.
    /// </summary>
    [HttpGet("list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFileList([FromQuery] string? path, [FromQuery] string pattern = "*.*", [FromQuery] string? user = null, [FromQuery] string? pass = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return LegacyApiResponse.Fail(this, "路径不能为空");
        }

        try
        {
            var files = await _fileAccessService.GetFileNamesAsync(path, pattern, user, pass, ct).ConfigureAwait(false);
            return LegacyApiResponse.Success(this, "查询成功", files);
        }
        catch (Exception ex)
        {
            return LegacyApiResponse.Fail(this, $"获取列表失败: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks whether a file exists.
    /// </summary>
    [HttpGet("exists")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult CheckFileExists([FromQuery] string? path, [FromQuery] string? user = null, [FromQuery] string? pass = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return LegacyApiResponse.Fail(this, "路径不能为空");
        }

        try
        {
            return LegacyApiResponse.Success(this, "查询成功", new { fileExists = _fileAccessService.Exists(path, user, pass) });
        }
        catch (Exception ex)
        {
            return LegacyApiResponse.Fail(this, $"失败: {ex.Message}");
        }
    }

    /// <summary>
    /// Downloads one file.
    /// </summary>
    [HttpGet("download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadFile([FromQuery] string? path, [FromQuery] string? user = null, [FromQuery] string? pass = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return LegacyApiResponse.Fail(this, "路径不能为空");
        }

        try
        {
            var file = await _fileAccessService.OpenReadAsync(path, user, pass, ct).ConfigureAwait(false);
            return File(file.Content, file.ContentType, file.FileName);
        }
        catch (FileNotFoundException)
        {
            return LegacyApiResponse.Fail(this, "文件不存在");
        }
        catch (Exception ex)
        {
            return LegacyApiResponse.Fail(this, $"文件下载失败: {ex.Message}");
        }
    }

    /// <summary>
    /// Uploads one file to the target path.
    /// </summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadFile([FromQuery] string? path, IFormFile? file, [FromQuery] string? user = null, [FromQuery] string? pass = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return LegacyApiResponse.Fail(this, "目标路径不能为空");
        }

        if (file == null)
        {
            return LegacyApiResponse.Fail(this, "未检测到上传文件");
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _fileAccessService.SaveAsync(path, file.FileName, file.Length, stream, user, pass, ct).ConfigureAwait(false);
            return LegacyApiResponse.Success(this, "上传成功", result);
        }
        catch (Exception ex)
        {
            return LegacyApiResponse.Fail(this, $"上传失败: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes one file.
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteFile([FromQuery] string? path, [FromQuery] string? user = null, [FromQuery] string? pass = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return LegacyApiResponse.Fail(this, "路径不能为空");
        }

        try
        {
            await _fileAccessService.DeleteAsync(path, user, pass, ct).ConfigureAwait(false);
            return LegacyApiResponse.Success(this, "删除成功");
        }
        catch (Exception ex)
        {
            return LegacyApiResponse.Fail(this, $"删除失败: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets parsed preview rows from a CSV or Excel file.
    /// </summary>
    [HttpGet("preview")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPreview([FromQuery] string? path, [FromQuery] int top = 10, [FromQuery] string? user = null, [FromQuery] string? pass = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return LegacyApiResponse.Fail(this, "路径不能为空");
        }

        try
        {
            var data = await _previewService.GetFilePreviewAsync(path, top, user, pass, ct).ConfigureAwait(false);
            return LegacyApiResponse.Success(this, $"成功获取前 {data.Count} 行数据预览", data);
        }
        catch (Exception ex)
        {
            return LegacyApiResponse.Fail(this, $"预览失败: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets detailed discovery results for one config.
    /// </summary>
    [HttpGet("discovery")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDetailedDiscovery([FromQuery] string? configId, [FromQuery] DateTime? startTime = null, [FromQuery] DateTime? endTime = null, [FromQuery] string? user = null, [FromQuery] string? pass = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(configId))
        {
            return LegacyApiResponse.Fail(this, "配置ID不能为空");
        }

        try
        {
            var config = _configService.GetByIds(new[] { configId }).FirstOrDefault();
            if (config == null)
            {
                return LegacyApiResponse.Fail(this, "未找到对应的采集配置");
            }

            var start = (startTime ?? DateTime.Now.AddDays(-7)).Date;
            var end = (endTime ?? DateTime.Now).Date;
            var list = await _discoveryService.GetDetailedDiscoveryAsync(config, start, end, user, pass, ct).ConfigureAwait(false);
            return LegacyApiResponse.Success(this, "获取巡检清单成功", list);
        }
        catch (Exception ex)
        {
            return LegacyApiResponse.Fail(this, $"巡检失败: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets one-day discovery results for all configs under a group.
    /// </summary>
    [HttpGet("group-discovery")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGroupDiscovery([FromQuery] int groupId, [FromQuery] DateTime? date, [FromQuery] string? user = null, [FromQuery] string? pass = null, CancellationToken ct = default)
    {
        if (groupId <= 0)
        {
            return LegacyApiResponse.Fail(this, "配置组ID不能为空");
        }

        if (date == null)
        {
            return LegacyApiResponse.Fail(this, "巡检日期不能为空");
        }

        try
        {
            var result = await _discoveryService.GetGroupDiscoveryAsync(groupId, date.Value, user, pass, ct).ConfigureAwait(false);
            return LegacyApiResponse.Success(this, "获取配置组巡检结果成功", result);
        }
        catch (Exception ex)
        {
            return LegacyApiResponse.Fail(this, $"配置组巡检失败: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets acquisition file states for one config and date range.
    /// </summary>
    [HttpGet("state")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFileState([FromQuery] int configId, [FromQuery] DateTime? businessDate = null, [FromQuery] DateTime? startTime = null, [FromQuery] DateTime? endTime = null, [FromQuery] string? fileName = null, CancellationToken ct = default)
    {
        if (configId <= 0)
        {
            return LegacyApiResponse.Fail(this, "配置ID不能为空");
        }

        var start = businessDate?.Date ?? startTime?.Date ?? DateTime.Now.AddDays(-7).Date;
        var end = businessDate?.Date ?? endTime?.Date ?? DateTime.Now.Date;
        if (end < start)
        {
            return LegacyApiResponse.Fail(this, "结束日期不能早于开始日期");
        }

        var states = await _fileStateService.GetByConfigAndDateRangeAsync(configId, start, end, ct).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            states = states.Where(x => string.Equals(x.FileName, fileName.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return LegacyApiResponse.Success(this, "获取文件状态成功", states);
    }
}

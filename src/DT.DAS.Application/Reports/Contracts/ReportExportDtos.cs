namespace DT.DAS.Application.Reports.Contracts;

public sealed class ReportExportCreateRequestDto
{
    public List<int>? GroupIds { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}

public sealed class ReportExportCreateResponseDto
{
    public string? ExportTaskId { get; set; }
}

public sealed class ReportExportTaskDto
{
    public string? Id { get; set; }
    public string? Status { get; set; }
    public int Progress { get; set; }
    public string? Stage { get; set; }
    public string? FileName { get; set; }
    public string? ErrorMessage { get; set; }
    public bool CanDownload { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiredAt { get; set; }
}

public sealed class ReportExportDownload
{
    public required string FilePath { get; init; }
    public required string FileName { get; init; }
    public string ContentType { get; init; } = "application/octet-stream";
}

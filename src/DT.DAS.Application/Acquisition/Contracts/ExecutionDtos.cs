namespace DT.DAS.Application.Acquisition.Contracts;

public sealed class TaskStartResponseDto
{
    public string? TaskLogId { get; set; }
    public string Status { get; set; } = "Running";
    public string Message { get; set; } = string.Empty;
}

public sealed class TaskStatusDto
{
    public string? TaskLogId { get; set; }
    public string? TaskCode { get; set; }
    public string? TriggerType { get; set; }
    public string? Status { get; set; }
    public int TotalConfigs { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public int ProcessedCount { get; set; }
    public int Progress { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? Message { get; set; }
}

public sealed class TaskDetailLogDto
{
    public string? Id { get; set; }
    public string? TaskLogId { get; set; }
    public int ConfigId { get; set; }
    public string? FileName { get; set; }
    public int StartRow { get; set; }
    public int ProcessedRows { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? Status { get; set; }
    public string? ErrorMessage { get; set; }
}



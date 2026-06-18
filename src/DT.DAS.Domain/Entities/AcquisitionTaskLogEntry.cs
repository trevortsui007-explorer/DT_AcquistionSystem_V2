namespace DT.DAS.Domain.Entities;

public sealed class AcquisitionTaskLogEntry
{
    public string? Id { get; set; }
    public int TaskId { get; set; }
    public string? TaskCode { get; set; }
    public string? TriggerType { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? Status { get; set; }
    public int TotalConfigs { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public int ProcessedCount { get; set; }
    public int Progress { get; set; }
    public string? Message { get; set; }
}


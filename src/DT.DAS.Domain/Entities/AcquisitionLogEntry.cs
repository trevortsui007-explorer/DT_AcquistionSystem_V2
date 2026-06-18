namespace DT.DAS.Domain.Entities;

public sealed class AcquisitionLogEntry
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


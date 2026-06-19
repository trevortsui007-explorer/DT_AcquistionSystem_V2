namespace DT.DAS.Domain.Entities;

public sealed class AcquisitionTask
{
    public int Id { get; set; }
    public string? TaskName { get; set; }
    public int TaskMode { get; set; }
    public string? CronExpression { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? Description { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
}

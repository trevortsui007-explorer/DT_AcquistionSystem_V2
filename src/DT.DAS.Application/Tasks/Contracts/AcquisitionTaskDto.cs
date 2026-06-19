namespace DT.DAS.Application.Tasks.Contracts;

public sealed class AcquisitionTaskDto
{
    public int Id { get; set; }
    public string? TaskName { get; set; }
    public int TaskMode { get; set; }
    public string? CronExpression { get; set; }
    public bool IsEnabled { get; set; }
    public string? Description { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
    public int GroupCount { get; set; }
    public IReadOnlyCollection<int> GroupIds { get; set; } = Array.Empty<int>();
    public IReadOnlyCollection<TaskAssociatedGroupDto> AssociatedGroups { get; set; } = Array.Empty<TaskAssociatedGroupDto>();
}

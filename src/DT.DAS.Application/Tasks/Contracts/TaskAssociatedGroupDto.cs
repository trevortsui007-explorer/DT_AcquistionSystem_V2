namespace DT.DAS.Application.Tasks.Contracts;

public sealed class TaskAssociatedGroupDto
{
    public int Id { get; set; }
    public string? GroupName { get; set; }
    public string? GroupCategory { get; set; }
    public string? GroupType { get; set; }
    public int ConfigCount { get; set; }
    public bool IsEnabled { get; set; }
}

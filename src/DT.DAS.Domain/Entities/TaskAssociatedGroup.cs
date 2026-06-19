namespace DT.DAS.Domain.Entities;

public sealed class TaskAssociatedGroup
{
    public int Id { get; set; }
    public string? GroupName { get; set; }
    public string? GroupCategory { get; set; }
    public string? GroupType { get; set; }
    public int ConfigCount { get; set; }
    public bool IsEnabled { get; set; }
}

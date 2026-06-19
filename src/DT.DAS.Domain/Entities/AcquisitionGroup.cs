namespace DT.DAS.Domain.Entities;

public sealed class AcquisitionGroup
{
    public int Id { get; set; }
    public string? GroupName { get; set; }
    public string? GroupCategory { get; set; }
    public string? GroupType { get; set; }
    public string? ExportProcedureName { get; set; }
    public bool IsEnabled { get; set; } = true;
}

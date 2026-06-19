namespace DT.DAS.Application.Configs.Contracts;

public sealed class AcquisitionGroupDto
{
    public int Id { get; set; }
    public string? GroupName { get; set; }
    public string? GroupCategory { get; set; }
    public string? GroupType { get; set; }
    public string? ExportProcedureName { get; set; }
    public bool IsEnabled { get; set; }
    public int ConfigCount { get; set; }
    public IReadOnlyCollection<AcquisitionConfigDto> AssociatedConfigs { get; set; } = Array.Empty<AcquisitionConfigDto>();
}

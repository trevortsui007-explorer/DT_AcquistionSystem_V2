namespace DT.DAS.Domain.Entities;

public sealed class AcquisitionConfigStatus
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool IsEnabled { get; set; }
}

namespace DT.DAS.Application.Acquisition.Contracts;

public sealed class AcquisitionSummary
{
    public int SuccessCount;
    public int FailureCount;
    public List<string> ErrorDetails { get; } = new();
}


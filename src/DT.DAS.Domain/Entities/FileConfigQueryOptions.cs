namespace DT.DAS.Domain.Entities;

public sealed class FileConfigQueryOptions
{
    public string? TableName { get; set; }
    public string? DatabaseName { get; set; }
    public IReadOnlyCollection<string>? Ids { get; set; }
    public IReadOnlyCollection<string>? GroupIds { get; set; }
    public IReadOnlyCollection<string>? TaskIds { get; set; }
}


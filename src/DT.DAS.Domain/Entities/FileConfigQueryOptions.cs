namespace DT.DAS.Domain.Entities;

public sealed class FileConfigQueryOptions
{
    public string? TableName { get; set; }
    public string? LinkTableName { get; set; }
    public string? DatabaseName { get; set; }
    public IReadOnlyCollection<string>? Ids { get; set; }
    public IReadOnlyCollection<string>? GroupIds { get; set; }
    public IReadOnlyCollection<string>? TaskIds { get; set; }

    public bool HasIdFilter => Ids is { Count: > 0 };
    public bool HasGroupFilter => GroupIds is { Count: > 0 };
    public bool HasTaskFilter => TaskIds is { Count: > 0 };
}

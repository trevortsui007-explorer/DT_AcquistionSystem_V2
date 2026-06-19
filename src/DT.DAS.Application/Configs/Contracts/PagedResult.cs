namespace DT.DAS.Application.Configs.Contracts;

public sealed class PagedResult<T>
{
    public int Total { get; set; }
    public IReadOnlyCollection<T> List { get; set; } = Array.Empty<T>();
}

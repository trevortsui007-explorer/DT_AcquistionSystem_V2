namespace DT.DAS.Domain.Interfaces;

public interface IDataParser
{
    List<T> Parse<T>(Stream stream, object? options = null) where T : class, new();
    Task<List<T>> ParseAsync<T>(Stream stream, object? options = null, CancellationToken ct = default) where T : class, new();
}


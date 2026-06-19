namespace DT.DAS.Application.Files;

public interface IDataPreviewService
{
    Task<List<Dictionary<string, object?>>> GetFilePreviewAsync(string path, int top = 10, string? user = null, string? pass = null, CancellationToken ct = default);
}

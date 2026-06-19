using DT.DAS.Application.Files.Contracts;

namespace DT.DAS.Application.Files;

public interface IFileAccessService
{
    Task<IEnumerable<string>> GetFileNamesAsync(string path, string pattern = "*.*", string? user = null, string? pass = null, CancellationToken ct = default);
    bool Exists(string path, string? user = null, string? pass = null);
    Task<FileDownloadResult> OpenReadAsync(string path, string? user = null, string? pass = null, CancellationToken ct = default);
    Task<FileUploadResult> SaveAsync(string path, string fileName, long size, Stream content, string? user = null, string? pass = null, CancellationToken ct = default);
    Task DeleteAsync(string path, string? user = null, string? pass = null, CancellationToken ct = default);
}

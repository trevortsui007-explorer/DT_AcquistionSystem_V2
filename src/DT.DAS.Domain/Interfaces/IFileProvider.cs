namespace DT.DAS.Domain.Interfaces;

public interface IFileProvider
{
    bool CanHandle(string path);
    bool Exists(string filePath);
    Task<Stream> GetFileStreamAsync(string filePath, CancellationToken cancellationToken = default);
    Task SaveFileAsync(string filePath, Stream content, bool overwrite = true, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetFileNamesAsync(string directoryPath, string searchPattern = "*.*", bool recursive = false, CancellationToken cancellationToken = default);
    Task MoveFileAsync(string sourcePath, string destinationPath, bool overwrite = false, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default);
}


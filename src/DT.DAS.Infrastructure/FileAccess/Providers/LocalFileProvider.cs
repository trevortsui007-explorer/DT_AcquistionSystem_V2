using DT.DAS.Domain.Interfaces;

namespace DT.DAS.Infrastructure.FileAccess.Providers;

public sealed class LocalFileProvider : IFileProvider
{
    public bool CanHandle(string path)
    {
        return !string.IsNullOrWhiteSpace(path) && (!path.Contains("://", StringComparison.Ordinal) || Path.IsPathRooted(path));
    }

    public bool Exists(string filePath)
    {
        return !string.IsNullOrWhiteSpace(filePath) && File.Exists(Path.GetFullPath(filePath));
    }

    public Task<Stream> GetFileStreamAsync(string filePath, CancellationToken cancellationToken = default)
    {
        Stream stream = new FileStream(Path.GetFullPath(filePath), FileMode.Open, System.IO.FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        return Task.FromResult(stream);
    }

    public async Task SaveFileAsync(string filePath, Stream content, bool overwrite = true, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.GetFullPath(filePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = new FileStream(fullPath, overwrite ? FileMode.Create : FileMode.CreateNew, System.IO.FileAccess.Write, FileShare.None, 4096, useAsync: true);
        await content.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    public Task<IEnumerable<string>> GetFileNamesAsync(string directoryPath, string searchPattern = "*.*", bool recursive = false, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.GetFullPath(directoryPath);
        if (!Directory.Exists(fullPath))
        {
            return Task.FromResult(Enumerable.Empty<string>());
        }

        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return Task.FromResult(Directory.EnumerateFiles(fullPath, searchPattern, option));
    }

    public Task MoveFileAsync(string sourcePath, string destinationPath, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        var source = Path.GetFullPath(sourcePath);
        var destination = Path.GetFullPath(destinationPath);
        var directory = Path.GetDirectoryName(destination);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (overwrite && File.Exists(destination))
        {
            File.Delete(destination);
        }

        File.Move(source, destination);
        return Task.CompletedTask;
    }

    public Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.GetFullPath(filePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}



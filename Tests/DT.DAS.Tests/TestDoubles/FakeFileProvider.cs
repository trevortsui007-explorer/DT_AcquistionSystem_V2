using System.Text;
using DT.DAS.Domain.Interfaces;

namespace DT.DAS.Tests.TestDoubles;

internal sealed class FakeFileProvider : IFileProvider
{
    private readonly Func<string, bool> _canHandle;
    private readonly Dictionary<string, byte[]> _files = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _deleted = new(StringComparer.OrdinalIgnoreCase);

    public FakeFileProvider(string name, Func<string, bool> canHandle)
    {
        Name = name;
        _canHandle = canHandle;
    }

    public string Name { get; }
    public IReadOnlyCollection<string> DeletedFiles => _deleted;
    public string LastSearchPattern { get; private set; } = "*.*";

    public void AddFile(string path, string content)
    {
        _files[path] = Encoding.UTF8.GetBytes(content);
    }

    public bool CanHandle(string path) => _canHandle(path);

    public bool Exists(string filePath) => _files.ContainsKey(filePath);

    public Task<Stream> GetFileStreamAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<Stream>(new MemoryStream(_files[filePath], writable: false));
    }

    public async Task SaveFileAsync(string filePath, Stream content, bool overwrite = true, CancellationToken cancellationToken = default)
    {
        if (!overwrite && _files.ContainsKey(filePath))
        {
            throw new IOException("File already exists.");
        }

        using var memory = new MemoryStream();
        await content.CopyToAsync(memory, cancellationToken).ConfigureAwait(false);
        _files[filePath] = memory.ToArray();
    }

    public Task<IEnumerable<string>> GetFileNamesAsync(string directoryPath, string searchPattern = "*.*", bool recursive = false, CancellationToken cancellationToken = default)
    {
        LastSearchPattern = searchPattern;
        var prefix = directoryPath.TrimEnd('/', '\\') + "/";
        var rows = _files.Keys
            .Where(x => x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || x.StartsWith(directoryPath.TrimEnd('/', '\\') + "\\", StringComparison.OrdinalIgnoreCase))
            .Where(x => Matches(Path.GetFileName(x), searchPattern))
            .ToArray();
        return Task.FromResult<IEnumerable<string>>(rows);
    }

    public Task MoveFileAsync(string sourcePath, string destinationPath, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        if (!_files.TryGetValue(sourcePath, out var bytes))
        {
            throw new FileNotFoundException("File not found.", sourcePath);
        }

        if (!overwrite && _files.ContainsKey(destinationPath))
        {
            throw new IOException("File already exists.");
        }

        _files[destinationPath] = bytes;
        _files.Remove(sourcePath);
        return Task.CompletedTask;
    }

    public Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        _deleted.Add(filePath);
        _files.Remove(filePath);
        return Task.CompletedTask;
    }

    private static bool Matches(string? fileName, string searchPattern)
    {
        if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(searchPattern) || searchPattern is "*" or "*.*")
        {
            return true;
        }

        return fileName.EndsWith(searchPattern.Replace("*", string.Empty, StringComparison.Ordinal), StringComparison.OrdinalIgnoreCase);
    }
}

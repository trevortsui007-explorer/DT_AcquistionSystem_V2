using DT.DAS.Domain.Interfaces;

namespace DT.DAS.Tests.TestDoubles;

internal sealed class FakeFileProvider : IFileProvider
{
    private readonly Func<string, bool> _canHandle;

    public FakeFileProvider(string name, Func<string, bool> canHandle)
    {
        Name = name;
        _canHandle = canHandle;
    }

    public string Name { get; }
    public bool CanHandle(string path) => _canHandle(path);
    public bool Exists(string filePath) => true;
    public Task<Stream> GetFileStreamAsync(string filePath, CancellationToken cancellationToken = default) => Task.FromResult<Stream>(Stream.Null);
    public Task SaveFileAsync(string filePath, Stream content, bool overwrite = true, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<IEnumerable<string>> GetFileNamesAsync(string directoryPath, string searchPattern = "*.*", bool recursive = false, CancellationToken cancellationToken = default) => Task.FromResult(Enumerable.Empty<string>());
    public Task MoveFileAsync(string sourcePath, string destinationPath, bool overwrite = false, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

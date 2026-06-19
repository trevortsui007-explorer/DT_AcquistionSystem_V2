using DT.DAS.Application.Files.Contracts;
using DT.DAS.Domain.Interfaces;

namespace DT.DAS.Application.Files.Services;

public sealed class FileAccessService : IFileAccessService
{
    private readonly IFileProviderFactory _factory;

    public FileAccessService(IFileProviderFactory factory)
    {
        _factory = factory;
    }

    public Task<IEnumerable<string>> GetFileNamesAsync(string path, string pattern = "*.*", string? user = null, string? pass = null, CancellationToken ct = default)
    {
        EnsurePath(path);
        var provider = _factory.Create(path, user, pass);
        return provider.GetFileNamesAsync(path, string.IsNullOrWhiteSpace(pattern) ? "*.*" : pattern, false, ct);
    }

    public bool Exists(string path, string? user = null, string? pass = null)
    {
        EnsurePath(path);
        return _factory.Create(path, user, pass).Exists(path);
    }

    public async Task<FileDownloadResult> OpenReadAsync(string path, string? user = null, string? pass = null, CancellationToken ct = default)
    {
        EnsurePath(path);
        var provider = _factory.Create(path, user, pass);
        if (!provider.Exists(path))
        {
            throw new FileNotFoundException("文件不存在", path);
        }

        var stream = await provider.GetFileStreamAsync(path, ct).ConfigureAwait(false);
        return new FileDownloadResult
        {
            Content = stream,
            FileName = Path.GetFileName(path),
            ContentType = "application/octet-stream"
        };
    }

    public async Task<FileUploadResult> SaveAsync(string path, string fileName, long size, Stream content, string? user = null, string? pass = null, CancellationToken ct = default)
    {
        EnsurePath(path);
        ArgumentNullException.ThrowIfNull(content);

        var provider = _factory.Create(path, user, pass);
        await provider.SaveFileAsync(path, content, true, ct).ConfigureAwait(false);
        return new FileUploadResult
        {
            Path = path,
            FileName = fileName,
            Size = size
        };
    }

    public Task DeleteAsync(string path, string? user = null, string? pass = null, CancellationToken ct = default)
    {
        EnsurePath(path);
        return _factory.Create(path, user, pass).DeleteFileAsync(path, ct);
    }

    private static void EnsurePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException("路径不能为空");
        }
    }
}

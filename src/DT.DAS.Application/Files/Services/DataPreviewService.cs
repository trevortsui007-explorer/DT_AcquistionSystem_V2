using DT.DAS.Domain.Interfaces;

namespace DT.DAS.Application.Files.Services;

public sealed class DataPreviewService : IDataPreviewService
{
    private const int DefaultTop = 10;
    private const int MaxTop = 200;
    private readonly IFileProviderFactory _fileFactory;
    private readonly IDataParserFactory _parserFactory;

    public DataPreviewService(IFileProviderFactory fileFactory, IDataParserFactory parserFactory)
    {
        _fileFactory = fileFactory;
        _parserFactory = parserFactory;
    }

    public async Task<List<Dictionary<string, object?>>> GetFilePreviewAsync(string path, int top = DefaultTop, string? user = null, string? pass = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException("路径不能为空");
        }

        var provider = _fileFactory.Create(path, user, pass);
        if (!provider.Exists(path))
        {
            throw new FileNotFoundException("未找到指定文件", path);
        }

        await using var stream = await provider.GetFileStreamAsync(path, ct).ConfigureAwait(false);
        var parser = _parserFactory.Create(path);
        var rows = await parser.ParseAsync<Dictionary<string, object?>>(stream, null, ct).ConfigureAwait(false);
        return rows.Take(NormalizeTop(top)).ToList();
    }

    public static int NormalizeTop(int top)
    {
        if (top <= 0)
        {
            return DefaultTop;
        }

        return Math.Min(top, MaxTop);
    }
}

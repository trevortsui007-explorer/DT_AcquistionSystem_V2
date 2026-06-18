using System.Text;
using DT.DAS.Domain.Entities;
using DT.DAS.Domain.Interfaces;

namespace DT.DAS.Infrastructure.Parsing.Parsers;

public sealed class CsvStreamParser : BaseStreamParser, IDataParser
{
    public List<T> Parse<T>(Stream stream, object? options = null) where T : class, new()
    {
        return ParseAsync<T>(stream, options).GetAwaiter().GetResult();
    }

    public async Task<List<T>> ParseAsync<T>(Stream stream, object? options = null, CancellationToken ct = default) where T : class, new()
    {
        var opt = options as CsvParserOptions ?? new CsvParserOptions();
        var result = new List<T>();
        using var reader = new StreamReader(stream, Encoding.UTF8, true, opt.BufferSize, leaveOpen: true);

        string[]? headers = null;
        var currentRow = 0;
        while (!reader.EndOfStream)
        {
            ct.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
            currentRow++;

            if (line == null)
            {
                continue;
            }

            if (currentRow == opt.HeaderRow)
            {
                headers = SplitLine(line, opt);
                continue;
            }

            if (currentRow < opt.StartRow || (opt.SkipEmptyLines && string.IsNullOrWhiteSpace(line)) || headers == null)
            {
                continue;
            }

            result.Add(MapToEntity<T>(headers, SplitLine(line, opt), currentRow, opt.HasExtFields, opt.FilePath));
        }

        return result;
    }

    private static string[] SplitLine(string line, CsvParserOptions options)
    {
        return line.Split(options.Separator).Select(x => options.TrimFields ? x.Trim() : x).ToArray();
    }
}


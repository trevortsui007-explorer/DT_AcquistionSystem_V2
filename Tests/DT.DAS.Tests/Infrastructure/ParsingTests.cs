using System.Text;
using DT.DAS.Domain.Entities;
using DT.DAS.Infrastructure.Parsing.Parsers;

namespace DT.DAS.Tests.Infrastructure;

public sealed class ParsingTests
{
    [Fact]
    public async Task CsvParser_keeps_source_metadata_and_start_row()
    {
        var parser = new CsvStreamParser();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Name,Value\nskip,0\nalpha,42\n"));

        var rows = await parser.ParseAsync<Dictionary<string, object?>>(stream, new CsvParserOptions
        {
            HeaderRow = 1,
            StartRow = 3,
            HasExtFields = true,
            FilePath = "sample.csv"
        });

        Assert.Single(rows);
        Assert.Equal("alpha", rows[0]["Name"]);
        Assert.Equal("42", rows[0]["Value"]);
        Assert.Equal("sample.csv", rows[0]["fullFilePath"]);
        Assert.Equal(3, rows[0]["row"]);
    }
}

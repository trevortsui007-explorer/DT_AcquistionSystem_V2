namespace DT.DAS.Domain.Entities;

public class ParserOptionsBase
{
    public int HeaderRow { get; set; } = 1;
    public int StartRow { get; set; } = 2;
    public bool HasExtFields { get; set; }
    public string? FilePath { get; set; }
    public bool SkipEmptyLines { get; set; } = true;
    public bool TrimFields { get; set; } = true;
}

public sealed class CsvParserOptions : ParserOptionsBase
{
    public string Separator { get; set; } = ",";
    public int BufferSize { get; set; } = 8192;
}

public sealed class ExcelParserOptions : ParserOptionsBase
{
    public string? SheetName { get; set; }
}


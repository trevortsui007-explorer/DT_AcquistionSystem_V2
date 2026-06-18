using DT.DAS.Domain.Interfaces;

namespace DT.DAS.Infrastructure.Parsing.Parsers;

public sealed class DataParserDescriptor : IDataParserDescriptor
{
    private readonly HashSet<string> _supportedExtensions;

    public DataParserDescriptor(IDataParser parser, IEnumerable<string> supportedExtensions)
    {
        Parser = parser;
        _supportedExtensions = supportedExtensions
            .Select(NormalizeExtension)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<string> SupportedExtensions => _supportedExtensions;

    public IDataParser Parser { get; }

    public bool CanHandle(string fileNameOrExtension)
    {
        var extension = Path.GetExtension(fileNameOrExtension);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = fileNameOrExtension;
        }

        return _supportedExtensions.Contains(NormalizeExtension(extension));
    }

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return string.Empty;
        }

        return extension.StartsWith(".", StringComparison.Ordinal) ? extension : $".{extension}";
    }
}


using DT.DAS.Domain.Interfaces;

namespace DT.DAS.Infrastructure.Parsing.Parsers;

public sealed class DataParserFactory : IDataParserFactory
{
    private readonly IEnumerable<IDataParserDescriptor> _descriptors;

    public DataParserFactory(IEnumerable<IDataParserDescriptor> descriptors)
    {
        _descriptors = descriptors;
    }

    public IDataParser Create(string fileNameOrExtension)
    {
        var descriptor = _descriptors.FirstOrDefault(x => x.CanHandle(fileNameOrExtension));
        if (descriptor != null)
        {
            return descriptor.Parser;
        }

        var extension = Path.GetExtension(fileNameOrExtension);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = fileNameOrExtension;
        }

        throw new NotSupportedException($"Unsupported data file extension: {extension}");
    }
}

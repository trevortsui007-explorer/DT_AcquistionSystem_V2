namespace DT.DAS.Domain.Interfaces;

public interface IDataParserDescriptor
{
    IReadOnlyCollection<string> SupportedExtensions { get; }
    IDataParser Parser { get; }
    bool CanHandle(string fileNameOrExtension);
}

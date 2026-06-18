namespace DT.DAS.Domain.Interfaces;

public interface IDataParserFactory
{
    IDataParser Create(string fileNameOrExtension);
}


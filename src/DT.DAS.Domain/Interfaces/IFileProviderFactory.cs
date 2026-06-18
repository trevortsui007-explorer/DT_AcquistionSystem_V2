namespace DT.DAS.Domain.Interfaces;

public interface IFileProviderFactory
{
    IFileProvider Create(string path, string? username = null, string? password = null);
}


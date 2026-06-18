using DT.DAS.Domain.Interfaces;

namespace DT.DAS.Infrastructure.FileAccess.Providers;

public sealed class FileProviderFactory : IFileProviderFactory
{
    private readonly IEnumerable<IFileProvider> _providers;

    public FileProviderFactory(IEnumerable<IFileProvider> providers)
    {
        _providers = providers;
    }

    public IFileProvider Create(string path, string? username = null, string? password = null)
    {
        var provider = _providers.FirstOrDefault(x => x.CanHandle(path));
        if (provider == null)
        {
            throw new NotSupportedException($"No file provider can handle path: {path}");
        }

        if (provider is ICredentialSupported credentialSupported && !string.IsNullOrWhiteSpace(username))
        {
            credentialSupported.SetCredentials(username, password);
        }

        return provider;
    }
}


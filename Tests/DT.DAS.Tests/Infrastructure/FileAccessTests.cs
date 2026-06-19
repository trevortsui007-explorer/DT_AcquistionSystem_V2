using DT.DAS.Domain.Interfaces;
using DT.DAS.Infrastructure.FileAccess.Providers;
using DT.DAS.Tests.TestDoubles;

namespace DT.DAS.Tests.Infrastructure;

public sealed class FileAccessTests
{
    [Fact]
    public void FileProviderFactory_routes_by_protocol()
    {
        var local = new FakeFileProvider("local", path => !path.Contains("://", StringComparison.Ordinal));
        var ftp = new FakeFileProvider("ftp", path => path.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase));
        var factory = new FileProviderFactory(new IFileProvider[] { local, ftp });

        Assert.Same(local, factory.Create("C:\\data\\a.csv"));
        Assert.Same(ftp, factory.Create("ftp://server/a.csv"));
    }
}

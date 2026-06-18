using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using DT.DAS.Infrastructure.Options;

namespace DT.DAS.Infrastructure.Persistence;

public interface ISqlConnectionFactory
{
    SqlConnection Create(string? connectionName = null);
}

public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly IConfiguration _configuration;
    private readonly DasDatabaseOptions _options;

    public SqlConnectionFactory(IConfiguration configuration, IOptions<DasDatabaseOptions> options)
    {
        _configuration = configuration;
        _options = options.Value;
    }

    public SqlConnection Create(string? connectionName = null)
    {
        var name = string.IsNullOrWhiteSpace(connectionName) ? _options.DefaultConnectionName : connectionName;
        var connectionString = _configuration.GetConnectionString(name);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Connection string '{name}' is not configured.");
        }

        return new SqlConnection(connectionString);
    }
}



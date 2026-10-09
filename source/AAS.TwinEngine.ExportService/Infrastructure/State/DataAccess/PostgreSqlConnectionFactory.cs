using System.Data.Common;

using AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

using Microsoft.Extensions.Options;

using Npgsql;

namespace AAS.TwinEngine.ExportService.Infrastructure.State.DataAccess;

public sealed class PostgreSqlConnectionFactory : IDbConnectionFactory
{
    private readonly StateStoreConfig _config;

    public PostgreSqlConnectionFactory(IOptions<ExportServiceConfig> options)
    {
        _config = options.Value.StateStore;

        if (string.IsNullOrWhiteSpace(_config.ConnectionString))
        {
            throw new InvalidOperationException(
                "ExportService:StateStore:ConnectionString is not configured.");
        }

        try
        {
            _ = new NpgsqlConnectionStringBuilder(_config.ConnectionString);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "ExportService:StateStore:ConnectionString is not a valid PostgreSQL connection string.", ex);
        }
    }

    public DbConnection CreateConnection() => new NpgsqlConnection(_config.ConnectionString);
}

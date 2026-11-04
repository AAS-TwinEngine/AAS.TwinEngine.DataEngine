using System.Data.Common;

using AAS.TwinEngine.ExportService.Infrastructure.State.DataAccess;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace AAS.TwinEngine.ExportService.Infrastructure.Monitoring;

public sealed class StateStoreHealthCheck(
    IDbConnectionFactory connectionFactory,
    ILogger<StateStoreHealthCheck> logger) : IHealthCheck
{
    private readonly IDbConnectionFactory _connectionFactory = connectionFactory;
    private readonly ILogger<StateStoreHealthCheck> _logger = logger;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = 5;
            _ = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex) when (ex is DbException or OperationCanceledException or TimeoutException)
        {
            _logger.LogWarning(ex, "Export state store health check failed.");
            return HealthCheckResult.Unhealthy("Export state store is unavailable.");
        }
    }
}

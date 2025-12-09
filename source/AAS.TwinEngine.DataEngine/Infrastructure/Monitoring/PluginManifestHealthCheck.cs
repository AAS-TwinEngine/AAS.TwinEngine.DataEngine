using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Monitoring;

public class PluginManifestHealthCheck(IPluginManifestHealthStatus pluginManifestHealthStatus) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (pluginManifestHealthStatus.IsHealthy)
        {
            return Task.FromResult(HealthCheckResult.Healthy());
        }

        return Task.FromResult(HealthCheckResult.Unhealthy());
    }
}



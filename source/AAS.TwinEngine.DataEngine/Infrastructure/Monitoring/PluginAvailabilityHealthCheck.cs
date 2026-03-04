using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Config;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Monitoring;

public sealed class PluginAvailabilityHealthCheck(ICreateClient clientFactory,
                                                  IOptions<PluginConfig> pluginConfig,
                                                  IPluginManifestHealthStatus pluginManifestHealthStatus,
                                                  ILogger<PluginAvailabilityHealthCheck> logger) : IHealthCheck
{
    private const string HealthEndpoint = "healthz";

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!pluginManifestHealthStatus.IsHealthy)
        {
            logger.LogWarning("Plugin manifest health status is unhealthy");
            return HealthCheckResult.Unhealthy();
        }

        if (pluginConfig?.Value?.Plugins == null || pluginConfig.Value.Plugins.Count == 0)
        {
            logger.LogError("Plugins not configured or empty");
            return HealthCheckResult.Unhealthy("No plugins configured");
        }

        var allHealthy = await CheckAllPluginsAsync(pluginConfig.Value.Plugins, cancellationToken).ConfigureAwait(false);

        return allHealthy
                   ? HealthCheckResult.Healthy()
                   : HealthCheckResult.Unhealthy();
    }

    private async Task<bool> CheckAllPluginsAsync(IList<Plugin> plugins, CancellationToken cancellationToken)
    {
        foreach (var plugin in plugins)
        {
            var isAvailable = await CheckSinglePluginAsync(plugin, cancellationToken).ConfigureAwait(false);

            if (!isAvailable)
            {
                return false;
            }
        }

        return true;
    }

    private async Task<bool> CheckSinglePluginAsync(Plugin plugin, CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = clientFactory.CreateClient($"{PluginConfig.HttpClientNamePrefix}{plugin.PluginName}");

            using var response = await httpClient
                .GetAsync(new Uri(HealthEndpoint, UriKind.Relative), cancellationToken)
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            logger.LogWarning("Plugin health check failed for {Plugin}. Status: {StatusCode}", plugin.PluginName, response.StatusCode);
            return false;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Plugin health check failed for {Plugin}", plugin.PluginName);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Plugin health check timed out for {Plugin}", plugin.PluginName);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Plugin health check failed for {Plugin}", plugin.PluginName);
            return false;
        }
    }
}

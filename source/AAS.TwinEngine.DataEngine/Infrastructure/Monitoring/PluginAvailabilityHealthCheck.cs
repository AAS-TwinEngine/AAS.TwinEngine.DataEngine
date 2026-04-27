using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Monitoring;

public sealed class PluginAvailabilityHealthCheck(ICreateClient clientFactory,
                                                  IOptions<PluginsConfig> pluginsConfig,
                                                  ILogger<PluginAvailabilityHealthCheck> logger) : IHealthCheck
{
    private const string HealthEndpoint = "healthz";

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (pluginsConfig?.Value?.Instances == null || pluginsConfig.Value.Instances.Count == 0)
        {
            logger.LogError("Plugins not configured or empty");
            return HealthCheckResult.Unhealthy("No plugins configured");
        }

        var allHealthy = await CheckAllPluginsAsync(pluginsConfig.Value.Instances, cancellationToken).ConfigureAwait(false);

        return allHealthy
                   ? HealthCheckResult.Healthy()
                   : HealthCheckResult.Unhealthy();
    }

    private async Task<bool> CheckAllPluginsAsync(IList<ServiceInstance> plugins, CancellationToken cancellationToken)
    {
        var tasks = plugins.Select(plugin => CheckSinglePluginAsync(plugin, cancellationToken)).ToList();
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        return results.All(healthy => healthy);
    }

    private async Task<bool> CheckSinglePluginAsync(ServiceInstance plugin, CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = clientFactory.CreateClient($"{HttpClientNames.PluginHealthCheckPrefix}{plugin.Name}");
            var healthEndpoint = string.IsNullOrWhiteSpace(plugin.HealthEndpoint)
                                    ? HealthEndpoint
                                    : plugin.HealthEndpoint;

            if (string.IsNullOrWhiteSpace(plugin.HealthEndpoint))
            {
                logger.LogWarning("HealthEndpoint is not configured for plugin {Plugin}. Falling back to default endpoint. Configure a dedicated health endpoint to avoid relying on defaults.", plugin.Name);
            }

            using var response = await httpClient
                .GetAsync(new Uri(healthEndpoint, UriKind.Relative), cancellationToken)
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            logger.LogWarning("Plugin health check failed for {Plugin}. Status: {StatusCode}", plugin.Name, response.StatusCode);
            return false;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Plugin health check failed for {Plugin}", plugin.Name);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Plugin health check timed out for {Plugin}", plugin.Name);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Plugin health check failed for {Plugin}", plugin.Name);
            return false;
        }
    }
}

using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Monitoring;

public sealed class TemplateRegistryHealthCheck(ICreateClient clientFactory,
                                                IOptions<AasEnvironmentConfig> aasEnvironment,
                                                ILogger<TemplateRegistryHealthCheck> logger) : IHealthCheck
{
    private readonly string _aasRegistryPath = aasEnvironment.Value.AasRegistryPath;
    private readonly string _subModelRegistryPath = aasEnvironment.Value.SubModelRegistryPath;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var aasTask = CheckEndpointAsync(AasEnvironmentConfig.AasRegistryHealthCheckHttpClientName, _aasRegistryPath, "aas-registry", cancellationToken);
        var submodelTask = CheckEndpointAsync(AasEnvironmentConfig.SubmodelRegistryHealthCheckHttpClientName, _subModelRegistryPath, "submodel-registry", cancellationToken);

        var results = await Task.WhenAll(aasTask, submodelTask).ConfigureAwait(false);

        if (!results[0])
        {
            logger.LogWarning("AAS Registry health status is unhealthy");
        }

        if (!results[1])
        {
            logger.LogWarning("Submodel Registry health status is unhealthy");
        }

        return results[0] && results[1]
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy();
    }

    private async Task<bool> CheckEndpointAsync(string clientName, string path, string endpointKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            logger.LogWarning("Endpoint {EndpointKey} path is not configured", endpointKey);
            return false;
        }

        var requestPath = $"{path}?limit=1";

        try
        {
            var httpClient = clientFactory.CreateClient(clientName);
            using var response = await httpClient
                .GetAsync(new Uri(requestPath, UriKind.Relative), cancellationToken)
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            logger.LogWarning("Template Registry Health check failed for {EndpointKey}. Status: {StatusCode}", endpointKey, response.StatusCode);
            return false;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Template Registry Health check failed for {EndpointKey}", endpointKey);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Template Registry Health check timed out for {EndpointKey}", endpointKey);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Template Registry Health check failed for {EndpointKey}", endpointKey);
            return false;
        }
    }
}

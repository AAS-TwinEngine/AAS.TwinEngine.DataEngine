using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Monitoring;

public sealed class TemplateRegistryHealthCheck(ICreateClient clientFactory,
                                                ILogger<TemplateRegistryHealthCheck> logger) : IHealthCheck
{
    private const string AasRegistryPath = ApiPaths.ShellDescriptors;
    private const string SubModelRegistryPath = ApiPaths.SubmodelDescriptors;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var aasTask = CheckEndpointAsync(HttpClientNames.AasRegistryHealthCheck, AasRegistryPath, "aas-registry", cancellationToken);
        var submodelTask = CheckEndpointAsync(HttpClientNames.SubmodelRegistryHealthCheck, SubModelRegistryPath, "submodel-registry", cancellationToken);

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

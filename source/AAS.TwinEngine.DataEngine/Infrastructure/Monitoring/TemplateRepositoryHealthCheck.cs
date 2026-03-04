using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Monitoring;

public sealed class TemplateRepositoryHealthCheck(ICreateClient clientFactory, IOptions<AasEnvironmentConfig> aasEnvironment, ILogger<TemplateRepositoryHealthCheck> logger) : IHealthCheck
{
    private readonly string _aasRepositoryPath = aasEnvironment.Value.AasRepositoryPath;
    private readonly string _subModelRepositoryPath = aasEnvironment.Value.SubModelRepositoryPath;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var aasHealthy = await CheckEndpointAsync(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName, _aasRepositoryPath, "aas-repository", cancellationToken).ConfigureAwait(false);

        if (!aasHealthy)
        {
            logger.LogWarning("AAS Repository health status is unhealthy");
            return HealthCheckResult.Unhealthy();
        }

        var submodelHealthy = await CheckEndpointAsync(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName, _subModelRepositoryPath, "submodel-repository", cancellationToken).ConfigureAwait(false);

        if (submodelHealthy)
        {
            return HealthCheckResult.Healthy();
        }

        logger.LogWarning("Submodel Repository health status is unhealthy");
        return HealthCheckResult.Unhealthy();
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
            using var response = await httpClient.GetAsync(new Uri(requestPath, UriKind.Relative), cancellationToken)
                                           .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            logger.LogWarning("Health check failed for {EndpointKey}. Status: {StatusCode}", endpointKey, response.StatusCode);
            return false;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Health check failed for {EndpointKey}", endpointKey);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Health check timed out for {EndpointKey}", endpointKey);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Health check failed for {EndpointKey}", endpointKey);
            return false;
        }
    }
}

using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Monitoring;

public sealed class TemplateRepositoryHealthCheck(ICreateClient clientFactory, ILogger<TemplateRepositoryHealthCheck> logger) : IHealthCheck
{
    private const string AasRepositoryPath = AasEnvironmentConfig.AasRepositoryPath;
    private const string SubModelRepositoryPath = AasEnvironmentConfig.SubModelRepositoryPath;
    private const string ConceptDescriptionPath = AasEnvironmentConfig.ConceptDescriptionPath;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var aasTask = CheckHealthEndpointAsync(AasEnvironmentConfig.SubmodelTemplateRepositoryHealthCheck, AasRepositoryPath, "aas-template-repository", cancellationToken);
        var submodelTask = CheckHealthEndpointAsync(AasEnvironmentConfig.AasTemplateRepositoryHealthCheck, SubModelRepositoryPath, "submodel-template-repository", cancellationToken);
        var conceptDiscriptorTask = CheckHealthEndpointAsync(AasEnvironmentConfig.ConceptDescriptorTemplateRepositoryHealthCheck, ConceptDescriptionPath, "concept-descriptor-template-repository", cancellationToken);

        var results = await Task.WhenAll(aasTask, submodelTask, conceptDiscriptorTask).ConfigureAwait(false);

        if (!results[0])
        {
            logger.LogWarning("AAS Repository health status is unhealthy");
        }

        if (!results[1])
        {
            logger.LogWarning("Submodel Repository health status is unhealthy");
        }

        if (!results[2])
        {
            logger.LogWarning("Concept Discriptor Repository health status is unhealthy");
        }

        return results[0] && results[1] && results[2]
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy();
    }

    private async Task<bool> CheckHealthEndpointAsync(string clientName, string path, string endpointKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            logger.LogWarning("Endpoint {EndpointKey} path is not configured.", endpointKey);
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

            logger.LogWarning("Template Repository Health check failed for {EndpointKey}. Status: {StatusCode}", endpointKey, response.StatusCode);
            return false;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Template Repository Health check failed for {EndpointKey}", endpointKey);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Template Repository Health check timed out for {EndpointKey}", endpointKey);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Template Repository Health check failed for {EndpointKey}", endpointKey);
            return false;
        }
    }
}

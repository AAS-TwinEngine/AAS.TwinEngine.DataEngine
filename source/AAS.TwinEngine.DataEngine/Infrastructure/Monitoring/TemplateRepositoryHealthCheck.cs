using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Monitoring;

public sealed class TemplateRepositoryHealthCheck(ICreateClient clientFactory, IOptions<TemplateManagementConfig> templateManagementConfig, ILogger<TemplateRepositoryHealthCheck> logger) : IHealthCheck
{
    private const string AasRepositoryPath = ApiPaths.Shells;
    private const string SubModelRepositoryPath = ApiPaths.Submodels;
    private const string ConceptDescriptionPath = ApiPaths.ConceptDescriptions;
    private const string DefaultHealthEndpoint = "/actuator/health";

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var aasTask = CheckHealthEndpointAsync(HttpClientNames.AasTemplateRepositoryHealthCheck, AasRepositoryPath, "aas-template-repository", templateManagementConfig.Value.AasTemplateRepository.HealthEndpoint, cancellationToken);
        var submodelTask = CheckHealthEndpointAsync(HttpClientNames.SubmodelTemplateRepositoryHealthCheck, SubModelRepositoryPath, "submodel-template-repository", templateManagementConfig.Value.SubmodelTemplateRepository.HealthEndpoint, cancellationToken);
        var conceptDiscriptorTask = CheckHealthEndpointAsync(HttpClientNames.ConceptDescriptorTemplateRepositoryHealthCheck, ConceptDescriptionPath, "concept-descriptor-template-repository", templateManagementConfig.Value.ConceptDescriptionTemplateRepository.HealthEndpoint, cancellationToken);

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

    private async Task<bool> CheckHealthEndpointAsync(string clientName, string path, string endpointKey, string healthEndpoint, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            logger.LogWarning("Endpoint {EndpointKey} path is not configured.", endpointKey);
            return false;
        }

        var requestPath = string.IsNullOrWhiteSpace(healthEndpoint)
                             ? DefaultHealthEndpoint
                             : healthEndpoint;

        if (string.IsNullOrWhiteSpace(healthEndpoint))
        {
            logger.LogWarning("HealthEndpoint is not configured for {EndpointKey}. Falling back to default endpoint. Configure a dedicated health endpoint to avoid relying on defaults.", endpointKey);
        }

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

using AAS.TwinEngine.ExportService.Infrastructure.Http.Authorization;
using AAS.TwinEngine.ExportService.Infrastructure.Monitoring;
using AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AAS.TwinEngine.ExportService.ServiceConfiguration;

public static class ExportServiceHealthCheckExtensions
{
    private static readonly TimeSpan HealthCheckTimeout = TimeSpan.FromSeconds(5);

    public static IHealthChecksBuilder AddExportServiceHealthChecks(
        this IServiceCollection services,
        ExportServiceConfig config)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(config);

        var healthChecks = services.AddHealthChecks()
            .AddCheck<StateStoreHealthCheck>(
                "state-store",
                tags: ["ready"],
                timeout: HealthCheckTimeout);

        foreach (var endpoint in GetActiveEndpoints(config))
        {
            var healthClientName = $"{endpoint.ClientName}.Health";

            _ = services.AddHttpClient(healthClientName, client =>
            {
                client.BaseAddress = new Uri(endpoint.Config.BaseUrl);
                client.Timeout = HealthCheckTimeout;
            }).AddHttpMessageHandler(serviceProvider => new BearerTokenHandler(
                serviceProvider.GetRequiredService<ITokenProvider>(),
                endpoint.ClientName));

            healthChecks.Add(new HealthCheckRegistration(
                endpoint.ClientName,
                serviceProvider => new EndpointHealthCheck(
                    serviceProvider.GetRequiredService<IHttpClientFactory>(),
                    serviceProvider.GetRequiredService<ILogger<EndpointHealthCheck>>(),
                    healthClientName,
                    endpoint.Config.HealthEndpoint),
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready"],
                timeout: HealthCheckTimeout));
        }

        return healthChecks;
    }

    private static IEnumerable<(string ClientName, EndpointConfig Config)> GetActiveEndpoints(
        ExportServiceConfig config)
    {
        var endpoints = new List<(string ClientName, EndpointConfig Config)>();
        AddIfActive(endpoints, HttpClientNames.SourceShellDescriptors, config.Sources.ShellDescriptors,
            HttpClientNames.TargetShellDescriptors, config.Targets.ShellDescriptors);
        AddIfActive(endpoints, HttpClientNames.SourceSubmodelDescriptors, config.Sources.SubmodelDescriptors,
            HttpClientNames.TargetSubmodelDescriptors, config.Targets.SubmodelDescriptors);
        AddIfActive(endpoints, HttpClientNames.SourceShells, config.Sources.Shells,
            HttpClientNames.TargetShells, config.Targets.Shells);
        AddIfActive(endpoints, HttpClientNames.SourceSubmodels, config.Sources.Submodels,
            HttpClientNames.TargetSubmodels, config.Targets.Submodels);
        AddIfActive(endpoints, HttpClientNames.SourceConceptDescriptions, config.Sources.ConceptDescriptions,
            HttpClientNames.TargetConceptDescriptions, config.Targets.ConceptDescriptions);
        return endpoints;
    }

    private static void AddIfActive(
        ICollection<(string ClientName, EndpointConfig Config)> endpoints,
        string sourceClientName,
        EndpointConfig source,
        string targetClientName,
        EndpointConfig target)
    {
        if (source.Enabled)
        {
            endpoints.Add((sourceClientName, source));
            endpoints.Add((targetClientName, target));
        }
    }
}

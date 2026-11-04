using System.Net.Http;

using AAS.TwinEngine.ExportService.ApplicationLogic.Exceptions;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace AAS.TwinEngine.ExportService.Infrastructure.Monitoring;

public sealed class EndpointHealthCheck(
    IHttpClientFactory httpClientFactory,
    ILogger<EndpointHealthCheck> logger,
    string clientName,
    string healthEndpoint) : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ILogger<EndpointHealthCheck> _logger = logger;
    private readonly string _clientName = clientName;
    private readonly string _healthEndpoint = healthEndpoint;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient(_clientName);
            using var response = await client.GetAsync(
                new Uri(_healthEndpoint, UriKind.Relative),
                cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy();
            }

            _logger.LogWarning(
                "Dependency {Dependency} health check returned HTTP {StatusCode}.",
                _clientName,
                (int)response.StatusCode);
            return HealthCheckResult.Unhealthy("Dependency health endpoint returned a non-success status.");
        }
        catch (Exception ex) when (
            ex is HttpRequestException or OperationCanceledException or AuthenticationFailedException)
        {
            _logger.LogWarning(ex, "Dependency {Dependency} health check failed.", _clientName);
            return HealthCheckResult.Unhealthy("Dependency health endpoint is unavailable.");
        }
    }
}

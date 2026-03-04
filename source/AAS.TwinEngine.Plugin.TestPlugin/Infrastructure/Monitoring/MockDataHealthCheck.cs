using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AAS.TwinEngine.Plugin.TestPlugin.Infrastructure.Monitoring;

public class MockDataHealthCheck(IHostEnvironment hostEnvironment) : IHealthCheck
{
    private const string MetadataFileName = "mock-metadata.json";
    private const string DataFileName = "mock-submodel-data.json";

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var dataFolder = Path.Combine(hostEnvironment.ContentRootPath, "Data");

        var metadataPath = Path.Combine(dataFolder, MetadataFileName);
        var dataPath = Path.Combine(dataFolder, DataFileName);

        var metadataExists = File.Exists(metadataPath);
        var dataExists = File.Exists(dataPath);

        var isHealthy = metadataExists && dataExists;

        if (isHealthy)
        {
            return Task.FromResult(HealthCheckResult.Healthy("Mock data files are available."));
        }

        return Task.FromResult(HealthCheckResult.Unhealthy("Mock data files are missing."));
    }
}

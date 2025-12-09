using AAS.TwinEngine.DataEngine.Infrastructure.Monitoring;

using Microsoft.Extensions.Diagnostics.HealthChecks;

using NSubstitute;

using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Monitoring;

public class PluginManifestHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_Returns_Healthy_When_Service_IsHealthy()
    {
        var healthStatus = Substitute.For<IPluginManifestHealthStatus>();
        healthStatus.IsHealthy.Returns(true);

        var sut = new PluginManifestHealthCheck(healthStatus);

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_Returns_Unhealthy_When_Service_IsUnhealthy()
    {
        var healthStatus = Substitute.For<IPluginManifestHealthStatus>();
        healthStatus.IsHealthy.Returns(false);

        var sut = new PluginManifestHealthCheck(healthStatus);

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }
}

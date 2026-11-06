using System.Net;

using AAS.TwinEngine.ExportService.Infrastructure.Monitoring;
using AAS.TwinEngine.ExportService.ServiceConfiguration;
using AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace AAS.TwinEngine.ExportService.UnitTests.Infrastructure.Monitoring;

public class EndpointHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenDependencyReturnsSuccess_IsHealthy()
    {
        // Arrange
        var factory = CreateHttpClientFactory(HttpStatusCode.NoContent);
        var sut = new EndpointHealthCheck(
            factory,
            NullLogger<EndpointHealthCheck>.Instance,
            "source.Health",
            "/healthz");

        // Act
        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenDependencyReturnsFailure_IsUnhealthy()
    {
        // Arrange
        var factory = CreateHttpClientFactory(HttpStatusCode.ServiceUnavailable);
        var sut = new EndpointHealthCheck(
            factory,
            NullLogger<EndpointHealthCheck>.Instance,
            "source.Health",
            "/healthz");

        // Act
        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Dependency health endpoint returned a non-success status.", result.Description);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void AddExportServiceHealthChecks_WhenSourceEnablementChanges_ControlsBothEndpointProbes(
        bool sourceEnabled, bool legacyTargetEnabled)
    {
        var config = new ExportServiceConfig();
        config.Sources.ConceptDescriptions.Enabled = false;
        config.Sources.Submodels.Enabled = false;
        config.Sources.SubmodelDescriptors.Enabled = false;
        config.Sources.ShellDescriptors.Enabled = false;
        config.Sources.Shells.Enabled = sourceEnabled;
        config.Targets.Shells.Enabled = legacyTargetEnabled;
        var services = new ServiceCollection();
        _ = services.AddExportServiceHealthChecks(config);
        using var provider = services.BuildServiceProvider();

        var registrations = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;

        Assert.Equal(sourceEnabled ? 3 : 1, registrations.Count);
        Assert.Equal(sourceEnabled ? 2 : 0, registrations.Count(registration => registration.Name != "state-store"));
    }

    private static IHttpClientFactory CreateHttpClientFactory(HttpStatusCode statusCode)
    {
        var factory = Substitute.For<IHttpClientFactory>();
        _ = factory.CreateClient("source.Health")
            .Returns(_ => new HttpClient(new StubHttpMessageHandler(statusCode))
            {
                BaseAddress = new Uri("https://source.test")
            });
        return factory;
    }

    private sealed class StubHttpMessageHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/healthz", request.RequestUri?.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(statusCode));
        }
    }
}

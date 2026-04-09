using System.Net;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;
using AAS.TwinEngine.DataEngine.Infrastructure.Monitoring;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

using NSubstitute;

using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Monitoring;

public class TemplateRegistryHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_Returns_Healthy_When_Registry_And_Submodel_Are_Healthy()
    {
        var clientFactory = Substitute.For<ICreateClient>();

        clientFactory.CreateClient(AasEnvironmentConfig.AasRegistryHealthCheck).Returns(CreateHttpClient(HttpStatusCode.OK));
        clientFactory.CreateClient(AasEnvironmentConfig.SubmodelRegistryHealthCheck).Returns(CreateHttpClient(HttpStatusCode.OK));

        var logger = Substitute.For<ILogger<TemplateRegistryHealthCheck>>();

        var sut = new TemplateRegistryHealthCheck(clientFactory, logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_Returns_Unhealthy_When_AasRegistry_Is_Unhealthy()
    {
        var clientFactory = Substitute.For<ICreateClient>();

        clientFactory.CreateClient(AasEnvironmentConfig.AasRegistryHealthCheck).Returns(CreateHttpClient(HttpStatusCode.InternalServerError));
        clientFactory.CreateClient(AasEnvironmentConfig.SubmodelRegistryHealthCheck).Returns(CreateHttpClient(HttpStatusCode.OK));

        var logger = Substitute.For<ILogger<TemplateRegistryHealthCheck>>();

        var sut = new TemplateRegistryHealthCheck(clientFactory, logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_Returns_Unhealthy_When_SubmodelRegistry_Is_Unhealthy()
    {
        var clientFactory = Substitute.For<ICreateClient>();

        clientFactory.CreateClient(AasEnvironmentConfig.AasRegistryHealthCheck).Returns(CreateHttpClient(HttpStatusCode.OK));
        clientFactory.CreateClient(AasEnvironmentConfig.SubmodelRegistryHealthCheck).Returns(CreateHttpClient(HttpStatusCode.InternalServerError));

        var logger = Substitute.For<ILogger<TemplateRegistryHealthCheck>>();

        var sut = new TemplateRegistryHealthCheck(clientFactory, logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_Returns_Unhealthy_When_AasRegistry_Request_Throws_HttpRequestException()
    {
        var clientFactory = Substitute.For<ICreateClient>();
        clientFactory.CreateClient(AasEnvironmentConfig.AasRegistryHealthCheck).Returns(CreateThrowingHttpClient(new HttpRequestException("network")));
        clientFactory.CreateClient(AasEnvironmentConfig.SubmodelRegistryHealthCheck).Returns(CreateHttpClient(HttpStatusCode.OK));

        var logger = Substitute.For<ILogger<TemplateRegistryHealthCheck>>();

        var sut = new TemplateRegistryHealthCheck(clientFactory, logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_Returns_Unhealthy_When_SubmodelRegistry_Request_Throws_TaskCanceledException()
    {
        var clientFactory = Substitute.For<ICreateClient>();

        clientFactory.CreateClient(AasEnvironmentConfig.AasRegistryHealthCheck).Returns(CreateHttpClient(HttpStatusCode.OK));
        clientFactory.CreateClient(AasEnvironmentConfig.SubmodelRegistryHealthCheck).Returns(CreateThrowingHttpClient(new TaskCanceledException("timeout")));

        var logger = Substitute.For<ILogger<TemplateRegistryHealthCheck>>();

        var sut = new TemplateRegistryHealthCheck(clientFactory, logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_Returns_Unhealthy_When_AasRegistry_Request_Throws_Exception()
    {
        var clientFactory = Substitute.For<ICreateClient>();
        clientFactory.CreateClient(AasEnvironmentConfig.AasRegistryHealthCheck)
            .Returns(CreateThrowingHttpClient(new Exception("unexpected")));
        clientFactory.CreateClient(AasEnvironmentConfig.SubmodelRegistryHealthCheck).Returns(CreateHttpClient(HttpStatusCode.OK));

        var logger = Substitute.For<ILogger<TemplateRegistryHealthCheck>>();

        var sut = new TemplateRegistryHealthCheck(clientFactory, logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_Checks_Both_Endpoints_In_Parallel_Even_When_AasRegistry_Is_Unhealthy()
    {
        var clientFactory = Substitute.For<ICreateClient>();
        clientFactory.CreateClient(AasEnvironmentConfig.AasRegistryHealthCheck).Returns(CreateHttpClient(HttpStatusCode.InternalServerError));
        clientFactory.CreateClient(AasEnvironmentConfig.SubmodelRegistryHealthCheck).Returns(CreateHttpClient(HttpStatusCode.OK));

        var logger = Substitute.For<ILogger<TemplateRegistryHealthCheck>>();

        var sut = new TemplateRegistryHealthCheck(clientFactory, logger);

        _ = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        clientFactory.Received(1).CreateClient(AasEnvironmentConfig.AasRegistryHealthCheck);
        clientFactory.Received(1).CreateClient(AasEnvironmentConfig.SubmodelRegistryHealthCheck);
    }

    [Fact]
    public async Task CheckHealthAsync_Uses_HealthCheck_Client_Names_Without_Retry_Policy()
    {
        var clientFactory = Substitute.For<ICreateClient>();
        clientFactory.CreateClient(AasEnvironmentConfig.AasRegistryHealthCheck).Returns(CreateHttpClient(HttpStatusCode.OK));
        clientFactory.CreateClient(AasEnvironmentConfig.SubmodelRegistryHealthCheck).Returns(CreateHttpClient(HttpStatusCode.OK));

        var logger = Substitute.For<ILogger<TemplateRegistryHealthCheck>>();

        var sut = new TemplateRegistryHealthCheck(clientFactory, logger);

        _ = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        clientFactory.Received().CreateClient(AasEnvironmentConfig.AasRegistryHealthCheck);
        clientFactory.Received().CreateClient(AasEnvironmentConfig.SubmodelRegistryHealthCheck);
        clientFactory.DidNotReceive().CreateClient(AasEnvironmentConfig.AasRegistry);
        clientFactory.DidNotReceive().CreateClient(AasEnvironmentConfig.SubmodelRegistry);
    }

    private static HttpClient CreateThrowingHttpClient(Exception exception)
    {
        var handler = new StubHttpMessageHandler((_, _) => throw exception);

        return new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost")
        };
    }

    private static HttpClient CreateHttpClient(HttpStatusCode statusCode)
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(statusCode)));

        return new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost")
        };
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => handler(request, cancellationToken);
    }
}

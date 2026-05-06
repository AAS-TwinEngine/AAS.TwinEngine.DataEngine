using System.Net;

using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;
using AAS.TwinEngine.DataEngine.Infrastructure.Monitoring;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Monitoring;

public class TemplateRegistryHealthCheckTests
{
    private static IOptions<TemplateManagementConfig> CreateDefaultOptions() =>
        Options.Create(new TemplateManagementConfig());

    private static IOptions<TemplateManagementConfig> CreateOptionsWithHealthEndpoints(string? aasEndpoint, string? submodelEndpoint)
    {
        var config = new TemplateManagementConfig
        {
            AasTemplateRegistry = new ServiceInstance { HealthEndpoint = aasEndpoint! },
            SubmodelTemplateRegistry = new ServiceInstance { HealthEndpoint = submodelEndpoint! }
        };
        return Options.Create(config);
    }

    [Fact]
    public async Task CheckHealthAsync_Returns_Healthy_When_Registry_And_Submodel_Are_Healthy()
    {
        var clientFactory = Substitute.For<ICreateClient>();

        clientFactory.CreateClient(HttpClientNames.AasRegistryHealthCheck).Returns(CreateHttpClient(HttpStatusCode.OK));
        clientFactory.CreateClient(HttpClientNames.SubmodelRegistryHealthCheck).Returns(CreateHttpClient(HttpStatusCode.OK));

        var logger = Substitute.For<ILogger<TemplateRegistryHealthCheck>>();

        var sut = new TemplateRegistryHealthCheck(clientFactory, CreateDefaultOptions(), logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_Returns_Unhealthy_When_AasRegistry_Is_Unhealthy()
    {
        var clientFactory = Substitute.For<ICreateClient>();

        clientFactory.CreateClient(HttpClientNames.AasRegistryHealthCheck).Returns(CreateHttpClient(HttpStatusCode.InternalServerError));
        clientFactory.CreateClient(HttpClientNames.SubmodelRegistryHealthCheck).Returns(CreateHttpClient(HttpStatusCode.OK));

        var logger = Substitute.For<ILogger<TemplateRegistryHealthCheck>>();

        var sut = new TemplateRegistryHealthCheck(clientFactory, CreateDefaultOptions(), logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_Returns_Unhealthy_When_SubmodelRegistry_Is_Unhealthy()
    {
        var clientFactory = Substitute.For<ICreateClient>();

        clientFactory.CreateClient(HttpClientNames.AasRegistryHealthCheck).Returns(CreateHttpClient(HttpStatusCode.OK));
        clientFactory.CreateClient(HttpClientNames.SubmodelRegistryHealthCheck).Returns(CreateHttpClient(HttpStatusCode.InternalServerError));

        var logger = Substitute.For<ILogger<TemplateRegistryHealthCheck>>();

        var sut = new TemplateRegistryHealthCheck(clientFactory, CreateDefaultOptions(), logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_Returns_Unhealthy_When_AasRegistry_Request_Throws_HttpRequestException()
    {
        var clientFactory = Substitute.For<ICreateClient>();
        clientFactory.CreateClient(HttpClientNames.AasRegistryHealthCheck).Returns(CreateThrowingHttpClient(new HttpRequestException("network")));
        clientFactory.CreateClient(HttpClientNames.SubmodelRegistryHealthCheck).Returns(CreateHttpClient(HttpStatusCode.OK));

        var logger = Substitute.For<ILogger<TemplateRegistryHealthCheck>>();

        var sut = new TemplateRegistryHealthCheck(clientFactory, CreateDefaultOptions(), logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_Returns_Unhealthy_When_SubmodelRegistry_Request_Throws_TaskCanceledException()
    {
        var clientFactory = Substitute.For<ICreateClient>();

        clientFactory.CreateClient(HttpClientNames.AasRegistryHealthCheck).Returns(CreateHttpClient(HttpStatusCode.OK));
        clientFactory.CreateClient(HttpClientNames.SubmodelRegistryHealthCheck).Returns(CreateThrowingHttpClient(new TaskCanceledException("timeout")));

        var logger = Substitute.For<ILogger<TemplateRegistryHealthCheck>>();

        var sut = new TemplateRegistryHealthCheck(clientFactory, CreateDefaultOptions(), logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_Returns_Unhealthy_When_AasRegistry_Request_Throws_Exception()
    {
        var clientFactory = Substitute.For<ICreateClient>();
        clientFactory.CreateClient(HttpClientNames.AasRegistryHealthCheck)
            .Returns(CreateThrowingHttpClient(new Exception("unexpected")));
        clientFactory.CreateClient(HttpClientNames.SubmodelRegistryHealthCheck).Returns(CreateHttpClient(HttpStatusCode.OK));

        var logger = Substitute.For<ILogger<TemplateRegistryHealthCheck>>();

        var sut = new TemplateRegistryHealthCheck(clientFactory, CreateDefaultOptions(), logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_Checks_Both_Endpoints_In_Parallel_Even_When_AasRegistry_Is_Unhealthy()
    {
        var clientFactory = Substitute.For<ICreateClient>();
        clientFactory.CreateClient(HttpClientNames.AasRegistryHealthCheck).Returns(CreateHttpClient(HttpStatusCode.InternalServerError));
        clientFactory.CreateClient(HttpClientNames.SubmodelRegistryHealthCheck).Returns(CreateHttpClient(HttpStatusCode.OK));

        var logger = Substitute.For<ILogger<TemplateRegistryHealthCheck>>();

        var sut = new TemplateRegistryHealthCheck(clientFactory, CreateDefaultOptions(), logger);

        _ = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        clientFactory.Received(1).CreateClient(HttpClientNames.AasRegistryHealthCheck);
        clientFactory.Received(1).CreateClient(HttpClientNames.SubmodelRegistryHealthCheck);
    }

    [Fact]
    public async Task CheckHealthAsync_Uses_HealthCheck_Client_Names_Without_Retry_Policy()
    {
        var clientFactory = Substitute.For<ICreateClient>();
        clientFactory.CreateClient(HttpClientNames.AasRegistryHealthCheck).Returns(CreateHttpClient(HttpStatusCode.OK));
        clientFactory.CreateClient(HttpClientNames.SubmodelRegistryHealthCheck).Returns(CreateHttpClient(HttpStatusCode.OK));

        var logger = Substitute.For<ILogger<TemplateRegistryHealthCheck>>();

        var sut = new TemplateRegistryHealthCheck(clientFactory, CreateDefaultOptions(), logger);

        _ = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        clientFactory.Received().CreateClient(HttpClientNames.AasRegistryHealthCheck);
        clientFactory.Received().CreateClient(HttpClientNames.SubmodelRegistryHealthCheck);
        clientFactory.DidNotReceive().CreateClient(HttpClientNames.AasRegistry);
        clientFactory.DidNotReceive().CreateClient(HttpClientNames.SubmodelRegistry);
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
        public List<Uri?> RequestedUris { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestedUris.Add(request.RequestUri);
            return handler(request, cancellationToken);
        }
    }

    [Fact]
    public async Task CheckHealthAsync_WhenHealthEndpointIsNull_UsesDefaultHealthEndpoint()
    {
        var handler = new StubHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };

        var clientFactory = Substitute.For<ICreateClient>();
        clientFactory.CreateClient(Arg.Any<string>()).Returns(client);
        var logger = Substitute.For<ILogger<TemplateRegistryHealthCheck>>();

        var sut = new TemplateRegistryHealthCheck(clientFactory, CreateOptionsWithHealthEndpoints(null, null), logger);

        await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.All(handler.RequestedUris, u => Assert.Contains("actuator/health", u!.AbsolutePath));
    }

    [Fact]
    public async Task CheckHealthAsync_WhenHealthEndpointIsEmpty_UsesDefaultHealthEndpoint()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };

        var clientFactory = Substitute.For<ICreateClient>();
        clientFactory.CreateClient(Arg.Any<string>()).Returns(client);
        var logger = Substitute.For<ILogger<TemplateRegistryHealthCheck>>();

        var sut = new TemplateRegistryHealthCheck(clientFactory, CreateOptionsWithHealthEndpoints(string.Empty, string.Empty), logger);

        await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.All(handler.RequestedUris, u => Assert.Contains("actuator/health", u!.AbsolutePath));
    }

    [Fact]
    public async Task CheckHealthAsync_WhenHealthEndpointIsBlank_LogsWarning()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };

        var clientFactory = Substitute.For<ICreateClient>();
        clientFactory.CreateClient(Arg.Any<string>()).Returns(client);
        var logger = Substitute.For<ILogger<TemplateRegistryHealthCheck>>();

        var sut = new TemplateRegistryHealthCheck(clientFactory, CreateOptionsWithHealthEndpoints(string.Empty, string.Empty), logger);

        await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        logger.Received(2).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>());
    }

    [Fact]
    public async Task CheckHealthAsync_WhenHealthEndpointIsConfigured_UsesConfiguredEndpoint()
    {
        const string customEndpoint = "actuator/health";
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };

        var clientFactory = Substitute.For<ICreateClient>();
        clientFactory.CreateClient(Arg.Any<string>()).Returns(client);
        var logger = Substitute.For<ILogger<TemplateRegistryHealthCheck>>();

        var sut = new TemplateRegistryHealthCheck(clientFactory, CreateOptionsWithHealthEndpoints(customEndpoint, customEndpoint), logger);

        await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.All(handler.RequestedUris, u => Assert.Contains(customEndpoint, u!.AbsolutePath));
    }

    [Fact]
    public async Task CheckHealthAsync_WhenHealthEndpointIsConfigured_ReturnsHealthy()
    {
        var clientFactory = Substitute.For<ICreateClient>();
        clientFactory.CreateClient(Arg.Any<string>()).Returns(CreateHttpClient(HttpStatusCode.OK));
        var logger = Substitute.For<ILogger<TemplateRegistryHealthCheck>>();

        var sut = new TemplateRegistryHealthCheck(clientFactory, CreateOptionsWithHealthEndpoints("actuator/health", "actuator/health"), logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenHealthEndpointIsConfigured_ReturnsUnhealthy_OnFailure()
    {
        var clientFactory = Substitute.For<ICreateClient>();
        clientFactory.CreateClient(Arg.Any<string>()).Returns(CreateHttpClient(HttpStatusCode.ServiceUnavailable));
        var logger = Substitute.For<ILogger<TemplateRegistryHealthCheck>>();

        var sut = new TemplateRegistryHealthCheck(clientFactory, CreateOptionsWithHealthEndpoints("actuator/health", "actuator/health"), logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenOneHealthEndpointMissing_UsesDefaultForMissingOne()
    {
        const string customEndpoint = "custom/health";

        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost")
        };

        var clientFactory = Substitute.For<ICreateClient>();
        clientFactory.CreateClient(Arg.Any<string>()).Returns(client);

        var logger = Substitute.For<ILogger<TemplateRegistryHealthCheck>>();

        var sut = new TemplateRegistryHealthCheck(
            clientFactory,
            CreateOptionsWithHealthEndpoints(customEndpoint, null),
            logger);

        await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(2, handler.RequestedUris.Count);

        Assert.Contains(handler.RequestedUris, u => u!.AbsolutePath.Contains(customEndpoint));
        Assert.Contains(handler.RequestedUris, u => u!.AbsolutePath.Contains("actuator/health"));
    }
}

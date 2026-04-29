using System.Net;

using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;
using AAS.TwinEngine.DataEngine.Infrastructure.Monitoring;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Monitoring;

public class TemplateRepositoryHealthCheckTests
{
    private readonly ICreateClient _clientFactory;
    private readonly ILogger<TemplateRepositoryHealthCheck> _logger;
    private readonly IOptions<TemplateManagementConfig> _options;

    public TemplateRepositoryHealthCheckTests()
    {
        _clientFactory = Substitute.For<ICreateClient>();
        _logger = Substitute.For<ILogger<TemplateRepositoryHealthCheck>>();
        _options = Options.Create(new TemplateManagementConfig());
    }

    private TemplateRepositoryHealthCheck CreateSut() => new(_clientFactory, _options, _logger);

    private TemplateRepositoryHealthCheck CreateSutWithHealthEndpoints(string? aasEndpoint, string? submodelEndpoint, string? conceptEndpoint)
    {
        var config = new TemplateManagementConfig
        {
            AasTemplateRepository = new ServiceInstance { HealthEndpoint = aasEndpoint! },
            SubmodelTemplateRepository = new ServiceInstance { HealthEndpoint = submodelEndpoint! },
            ConceptDescriptionTemplateRepository = new ServiceInstance { HealthEndpoint = conceptEndpoint! }
        };
        return new(_clientFactory, Options.Create(config), _logger);
    }

    private static HttpClient CreateHttpClient(HttpStatusCode statusCode)
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(statusCode));

        return new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost")
        };
    }

    private static HttpClient CreateHttpClientThatThrows(Exception ex)
    {
        var handler = new ExceptionHttpMessageHandler(ex);

        return new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost")
        };
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;
        public List<Uri?> RequestedUris { get; } = [];

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) => _handler = handler;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestedUris.Add(request.RequestUri);
            return Task.FromResult(_handler(request));
        }
    }

    private sealed class ExceptionHttpMessageHandler(Exception exception) : HttpMessageHandler
    {
        private readonly Exception _exception = exception;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => throw _exception;
    }

    [Fact]
    public async Task CheckHealthAsync_AllRepositoriesHealthy_ReturnsHealthy()
    {
        var client = CreateHttpClient(HttpStatusCode.OK);
        _clientFactory.CreateClient(Arg.Any<string>()).Returns(client);

        var sut = CreateSut();

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_OneRepositoryFails_ReturnsUnhealthy()
    {
        var successClient = CreateHttpClient(HttpStatusCode.OK);
        var failClient = CreateHttpClient(HttpStatusCode.InternalServerError);

        _clientFactory.CreateClient(HttpClientNames.SubmodelTemplateRepositoryHealthCheck)
            .Returns(successClient);

        _clientFactory.CreateClient(HttpClientNames.AasTemplateRepositoryHealthCheck)
            .Returns(failClient);

        _clientFactory.CreateClient(HttpClientNames.ConceptDescriptorTemplateRepositoryHealthCheck)
            .Returns(successClient);

        var sut = CreateSut();

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_AllRepositoriesFail_ReturnsUnhealthy()
    {
        var failClient = CreateHttpClient(HttpStatusCode.InternalServerError);
        _clientFactory.CreateClient(Arg.Any<string>()).Returns(failClient);

        var sut = CreateSut();

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenHttpRequestException_ReturnsUnhealthy()
    {
        var client = CreateHttpClientThatThrows(new HttpRequestException());
        _clientFactory.CreateClient(Arg.Any<string>()).Returns(client);

        var sut = CreateSut();

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenTimeout_ReturnsUnhealthy()
    {
        var client = CreateHttpClientThatThrows(new TaskCanceledException());
        _clientFactory.CreateClient(Arg.Any<string>()).Returns(client);

        var sut = CreateSut();

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenGenericException_ReturnsUnhealthy()
    {
        var client = CreateHttpClientThatThrows(new Exception("boom"));
        _clientFactory.CreateClient(Arg.Any<string>()).Returns(client);

        var sut = CreateSut();

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_NonSuccessStatusCodes_ReturnUnhealthy()
    {
        var client = CreateHttpClient(HttpStatusCode.NotFound);
        _clientFactory.CreateClient(Arg.Any<string>()).Returns(client);

        var sut = CreateSut();

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_LogsWarning_WhenRepositoryFails()
    {
        var failClient = CreateHttpClient(HttpStatusCode.InternalServerError);
        _clientFactory.CreateClient(Arg.Any<string>()).Returns(failClient);

        var sut = CreateSut();

        await sut.CheckHealthAsync(new HealthCheckContext());

        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>());
    }

    [Fact]
    public async Task CheckHealthAsync_WhenHealthEndpointIsNull_UsesDefaultHealthEndpoint()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        _clientFactory.CreateClient(Arg.Any<string>()).Returns(client);

        var sut = CreateSutWithHealthEndpoints(null, null, null);

        await sut.CheckHealthAsync(new HealthCheckContext());

        Assert.All(handler.RequestedUris, u => Assert.Contains("actuator/health", u!.AbsolutePath));
    }

    [Fact]
    public async Task CheckHealthAsync_WhenHealthEndpointIsEmpty_UsesDefaultHealthEndpoint()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        _clientFactory.CreateClient(Arg.Any<string>()).Returns(client);

        var sut = CreateSutWithHealthEndpoints(string.Empty, string.Empty, string.Empty);

        await sut.CheckHealthAsync(new HealthCheckContext());

        Assert.All(handler.RequestedUris, u => Assert.Contains("actuator/health", u!.AbsolutePath));
    }

    [Fact]
    public async Task CheckHealthAsync_WhenHealthEndpointIsBlank_LogsWarning()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        _clientFactory.CreateClient(Arg.Any<string>()).Returns(client);

        var sut = CreateSutWithHealthEndpoints(string.Empty, string.Empty, string.Empty);

        await sut.CheckHealthAsync(new HealthCheckContext());

        _logger.Received(3).Log(
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
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        _clientFactory.CreateClient(Arg.Any<string>()).Returns(client);

        var sut = CreateSutWithHealthEndpoints(customEndpoint, customEndpoint, customEndpoint);

        await sut.CheckHealthAsync(new HealthCheckContext());

        Assert.All(handler.RequestedUris, u => Assert.Contains(customEndpoint, u!.AbsolutePath));
    }

    [Fact]
    public async Task CheckHealthAsync_WhenHealthEndpointIsConfigured_ReturnsHealthy()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        _clientFactory.CreateClient(Arg.Any<string>()).Returns(client);

        var sut = CreateSutWithHealthEndpoints("actuator/health", "actuator/health", "actuator/health");

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenHealthEndpointIsConfigured_ReturnsUnhealthy_OnFailure()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        _clientFactory.CreateClient(Arg.Any<string>()).Returns(client);

        var sut = CreateSutWithHealthEndpoints("actuator/health", "actuator/health", "actuator/health");

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenSomeHealthEndpointsMissing_UsesDefaultForMissingOnes()
    {
        const string customEndpoint = "custom/health";

        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost")
        };

        _clientFactory.CreateClient(Arg.Any<string>()).Returns(client);

        var sut = CreateSutWithHealthEndpoints(customEndpoint, customEndpoint, null);

        await sut.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(3, handler.RequestedUris.Count);

        Assert.Contains(handler.RequestedUris, u => u!.AbsolutePath.Contains(customEndpoint));
        Assert.Contains(handler.RequestedUris, u => u!.AbsolutePath.Contains("actuator/health"));
    }
}

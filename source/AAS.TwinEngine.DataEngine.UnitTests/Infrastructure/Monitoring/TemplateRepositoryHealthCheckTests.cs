using System.Net;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;
using AAS.TwinEngine.DataEngine.Infrastructure.Monitoring;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Monitoring;

public class TemplateRepositoryHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_Returns_Healthy_When_Repository_And_Submodel_Are_Healthy()
    {
        var environmentConfig = new AasEnvironmentConfig
        {
            AasRepositoryPath = "/aas-repo",
            SubModelRepositoryPath = "/submodel-repo"
        };

        var options = Options.Create(environmentConfig);

        var clientFactory = Substitute.For<ICreateClient>();

        var handler = new SequenceHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK),
            new HttpResponseMessage(HttpStatusCode.OK));

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost")
        };

        clientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName).Returns(httpClient);

        var logger = Substitute.For<ILogger<TemplateRepositoryHealthCheck>>();

        var sut = new TemplateRepositoryHealthCheck(clientFactory, options, logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_Returns_Unhealthy_When_Repository_Is_Unhealthy()
    {
        var environmentConfig = new AasEnvironmentConfig
        {
            AasRepositoryPath = "/aas-repo",
            SubModelRepositoryPath = "/submodel-repo"
        };

        var options = Options.Create(environmentConfig);

        var clientFactory = Substitute.For<ICreateClient>();

        var handler = new SequenceHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.InternalServerError),
            new HttpResponseMessage(HttpStatusCode.OK));

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost")
        };

        clientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName).Returns(httpClient);

        var logger = Substitute.For<ILogger<TemplateRepositoryHealthCheck>>();

        var sut = new TemplateRepositoryHealthCheck(clientFactory, options, logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_Returns_Unhealthy_When_SubmodelRepository_Is_Unhealthy()
    {
        var environmentConfig = new AasEnvironmentConfig
        {
            AasRepositoryPath = "/aas-repo",
            SubModelRepositoryPath = "/submodel-repo"
        };

        var options = Options.Create(environmentConfig);

        var clientFactory = Substitute.For<ICreateClient>();

        var handler = new SequenceHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK),
            new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost")
        };

        clientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName).Returns(httpClient);

        var logger = Substitute.For<ILogger<TemplateRepositoryHealthCheck>>();

        var sut = new TemplateRepositoryHealthCheck(clientFactory, options, logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_Returns_Unhealthy_When_Repository_Path_Is_Not_Configured()
    {
        var environmentConfig = new AasEnvironmentConfig
        {
            AasRepositoryPath = string.Empty,
            SubModelRepositoryPath = "/submodel-repo"
        };

        var options = Options.Create(environmentConfig);

        var clientFactory = Substitute.For<ICreateClient>();

        var logger = Substitute.For<ILogger<TemplateRepositoryHealthCheck>>();

        var sut = new TemplateRepositoryHealthCheck(clientFactory, options, logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_Returns_Unhealthy_When_SubmodelRepository_Path_Is_Not_Configured()
    {
        var environmentConfig = new AasEnvironmentConfig
        {
            AasRepositoryPath = "/aas-repo",
            SubModelRepositoryPath = string.Empty
        };

        var options = Options.Create(environmentConfig);

        var clientFactory = Substitute.For<ICreateClient>();
        clientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName).Returns(CreateHttpClient(HttpStatusCode.OK));

        var logger = Substitute.For<ILogger<TemplateRepositoryHealthCheck>>();

        var sut = new TemplateRepositoryHealthCheck(clientFactory, options, logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_Returns_Unhealthy_When_Repository_Request_Throws_HttpRequestException()
    {
        var environmentConfig = new AasEnvironmentConfig
        {
            AasRepositoryPath = "/aas-repo",
            SubModelRepositoryPath = "/submodel-repo"
        };

        var options = Options.Create(environmentConfig);

        var clientFactory = Substitute.For<ICreateClient>();
        clientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName).Returns(CreateThrowingHttpClient(new HttpRequestException("network")));

        var logger = Substitute.For<ILogger<TemplateRepositoryHealthCheck>>();

        var sut = new TemplateRepositoryHealthCheck(clientFactory, options, logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_Returns_Unhealthy_When_Repository_Request_Throws_Unexpected_Exception()
    {
        var environmentConfig = new AasEnvironmentConfig
        {
            AasRepositoryPath = "/aas-repo",
            SubModelRepositoryPath = "/submodel-repo"
        };

        var options = Options.Create(environmentConfig);

        var clientFactory = Substitute.For<ICreateClient>();
        clientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName)
                     .Returns(CreateThrowingHttpClient(new InvalidOperationException("unexpected")));

        var logger = Substitute.For<ILogger<TemplateRepositoryHealthCheck>>();

        var sut = new TemplateRepositoryHealthCheck(clientFactory, options, logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_Returns_Unhealthy_When_SubmodelRepository_Request_Throws_TaskCanceledException()
    {
        var environmentConfig = new AasEnvironmentConfig
        {
            AasRepositoryPath = "/aas-repo",
            SubModelRepositoryPath = "/submodel-repo"
        };

        var options = Options.Create(environmentConfig);

        var clientFactory = Substitute.For<ICreateClient>();

        var handler = new SequenceHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK));

        clientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName)
                     .Returns(
                        new HttpClient(handler)
                        {
                            BaseAddress = new Uri("http://localhost")
                        },
                        CreateThrowingHttpClient(new TaskCanceledException("timeout")));

        var logger = Substitute.For<ILogger<TemplateRepositoryHealthCheck>>();

        var sut = new TemplateRepositoryHealthCheck(clientFactory, options, logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_Does_Not_Check_Submodel_When_Repository_Is_Unhealthy()
    {
        var environmentConfig = new AasEnvironmentConfig
        {
            AasRepositoryPath = "/aas-repo",
            SubModelRepositoryPath = "/submodel-repo"
        };

        var options = Options.Create(environmentConfig);

        var clientFactory = Substitute.For<ICreateClient>();

        var handler = new SequenceHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost")
        };

        clientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName).Returns(httpClient);

        var logger = Substitute.For<ILogger<TemplateRepositoryHealthCheck>>();

        var sut = new TemplateRepositoryHealthCheck(clientFactory, options, logger);

        _ = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        clientFactory.Received(1).CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName);
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

    private static HttpClient CreateThrowingHttpClient(Exception exception)
    {
        var handler = new StubHttpMessageHandler((_, _) => throw exception);

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

    private sealed class SequenceHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;

        public SequenceHttpMessageHandler(params HttpResponseMessage[] responses)
        {
            _responses = new Queue<HttpResponseMessage>(responses);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_responses.Count == 0)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }

            return Task.FromResult(_responses.Dequeue());
        }
    }
}

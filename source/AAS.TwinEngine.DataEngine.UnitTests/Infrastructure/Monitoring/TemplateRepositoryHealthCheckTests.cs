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

        clientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHealthCheckHttpClientName)
            .Returns(
                _ => CreateHttpClient(HttpStatusCode.OK),
                _ => CreateHttpClient(HttpStatusCode.OK));

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

        clientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHealthCheckHttpClientName)
            .Returns(
                _ => CreateHttpClient(HttpStatusCode.InternalServerError),
                _ => CreateHttpClient(HttpStatusCode.OK));

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

        clientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHealthCheckHttpClientName)
            .Returns(
                _ => CreateHttpClient(HttpStatusCode.OK),
                _ => CreateHttpClient(HttpStatusCode.InternalServerError));

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
        clientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHealthCheckHttpClientName)
            .Returns(_ => CreateHttpClient(HttpStatusCode.OK));

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
        clientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHealthCheckHttpClientName)
            .Returns(_ => CreateHttpClient(HttpStatusCode.OK));

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
        clientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHealthCheckHttpClientName)
            .Returns(
                _ => CreateThrowingHttpClient(new HttpRequestException("network")),
                _ => CreateHttpClient(HttpStatusCode.OK));

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
        clientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHealthCheckHttpClientName)
                     .Returns(
                         _ => CreateThrowingHttpClient(new InvalidOperationException("unexpected")),
                         _ => CreateHttpClient(HttpStatusCode.OK));

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
        clientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHealthCheckHttpClientName)
            .Returns(
                _ => CreateHttpClient(HttpStatusCode.OK),
                _ => CreateThrowingHttpClient(new TaskCanceledException("timeout")));

        var logger = Substitute.For<ILogger<TemplateRepositoryHealthCheck>>();

        var sut = new TemplateRepositoryHealthCheck(clientFactory, options, logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_Checks_Both_Endpoints_In_Parallel_Even_When_First_Is_Unhealthy()
    {
        var environmentConfig = new AasEnvironmentConfig
        {
            AasRepositoryPath = "/aas-repo",
            SubModelRepositoryPath = "/submodel-repo"
        };

        var options = Options.Create(environmentConfig);

        var clientFactory = Substitute.For<ICreateClient>();

        clientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHealthCheckHttpClientName)
            .Returns(
                _ => CreateHttpClient(HttpStatusCode.InternalServerError),
                _ => CreateHttpClient(HttpStatusCode.OK));

        var logger = Substitute.For<ILogger<TemplateRepositoryHealthCheck>>();

        var sut = new TemplateRepositoryHealthCheck(clientFactory, options, logger);

        _ = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        clientFactory.Received(2).CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHealthCheckHttpClientName);
    }

    [Fact]
    public async Task CheckHealthAsync_Uses_HealthCheck_Client_Name_Without_Retry_Policy()
    {
        var environmentConfig = new AasEnvironmentConfig
        {
            AasRepositoryPath = "/aas-repo",
            SubModelRepositoryPath = "/submodel-repo"
        };

        var options = Options.Create(environmentConfig);

        var clientFactory = Substitute.For<ICreateClient>();
        clientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHealthCheckHttpClientName)
            .Returns(
                _ => CreateHttpClient(HttpStatusCode.OK),
                _ => CreateHttpClient(HttpStatusCode.OK));

        var logger = Substitute.For<ILogger<TemplateRepositoryHealthCheck>>();

        var sut = new TemplateRepositoryHealthCheck(clientFactory, options, logger);

        _ = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        clientFactory.Received(2).CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHealthCheckHttpClientName);
        clientFactory.DidNotReceive().CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName);
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
}

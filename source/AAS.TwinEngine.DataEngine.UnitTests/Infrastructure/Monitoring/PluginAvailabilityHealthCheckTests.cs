using System.Net;

using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;
using AAS.TwinEngine.DataEngine.Infrastructure.Monitoring;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Monitoring;

public class PluginAvailabilityHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_Returns_Unhealthy_When_No_Plugins_Configured()
    {
        var clientFactory = Substitute.For<ICreateClient>();

        var pluginConfig = Options.Create(new PluginsConfig
        {
            Instances = []
        });

        var logger = Substitute.For<ILogger<PluginAvailabilityHealthCheck>>();

        var sut = new PluginAvailabilityHealthCheck(clientFactory, pluginConfig, logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_Returns_Unhealthy_When_Plugin_List_Is_Null()
    {
        var clientFactory = Substitute.For<ICreateClient>();

        var pluginConfig = Options.Create(new PluginsConfig
        {
            Instances = null!
        });

        var logger = Substitute.For<ILogger<PluginAvailabilityHealthCheck>>();

        var sut = new PluginAvailabilityHealthCheck(clientFactory, pluginConfig, logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_Returns_Healthy_When_All_Plugins_Are_Healthy()
    {
        var clientFactory = Substitute.For<ICreateClient>();

        clientFactory
            .CreateClient(Arg.Any<string>())
            .Returns(callInfo =>
            {
                var handler = new StubHttpMessageHandler((_, _) =>
                    Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

                return new HttpClient(handler)
                {
                    BaseAddress = new Uri("http://localhost")
                };
            });

        var pluginConfig = Options.Create(new PluginsConfig
        {
            Instances =
            [
                new ServiceInstance { Name = "Plugin1", BaseUrl = new Uri("http://localhost") },
                new ServiceInstance { Name = "Plugin2", BaseUrl = new Uri("http://localhost") }
            ]
        });

        var logger = Substitute.For<ILogger<PluginAvailabilityHealthCheck>>();

        var sut = new PluginAvailabilityHealthCheck(clientFactory, pluginConfig, logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_Returns_Unhealthy_When_Any_Plugin_Is_Unhealthy()
    {
        var clientFactory = Substitute.For<ICreateClient>();

        clientFactory
            .CreateClient(Arg.Any<string>())
            .Returns(callInfo =>
            {
                var handler = new StubHttpMessageHandler((_, _) =>
                    Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));

                return new HttpClient(handler)
                {
                    BaseAddress = new Uri("http://localhost")
                };
            });

        var pluginConfig = Options.Create(new PluginsConfig
        {
            Instances =
            [
                new ServiceInstance { Name = "Plugin1", BaseUrl = new Uri("http://localhost") }
            ]
        });

        var logger = Substitute.For<ILogger<PluginAvailabilityHealthCheck>>();

        var sut = new PluginAvailabilityHealthCheck(clientFactory, pluginConfig, logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_Returns_Unhealthy_When_Plugin_Request_Throws_HttpRequestException()
    {
        var clientFactory = Substitute.For<ICreateClient>();

        clientFactory
            .CreateClient(Arg.Any<string>())
            .Returns(_ =>
            {
                var handler = new StubHttpMessageHandler((_, _) => throw new HttpRequestException("network"));
                return new HttpClient(handler)
                {
                    BaseAddress = new Uri("http://localhost")
                };
            });

        var pluginConfig = Options.Create(new PluginsConfig
        {
            Instances =
            [
                new ServiceInstance { Name = "Plugin1", BaseUrl = new Uri("http://localhost") }
            ]
        });

        var logger = Substitute.For<ILogger<PluginAvailabilityHealthCheck>>();

        var sut = new PluginAvailabilityHealthCheck(clientFactory, pluginConfig, logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_Returns_Unhealthy_When_Plugin_Request_Throws_TaskCanceledException()
    {
        var clientFactory = Substitute.For<ICreateClient>();

        clientFactory
            .CreateClient(Arg.Any<string>())
            .Returns(_ =>
            {
                var handler = new StubHttpMessageHandler((_, _) => throw new TaskCanceledException("timeout"));
                return new HttpClient(handler)
                {
                    BaseAddress = new Uri("http://localhost")
                };
            });

        var pluginConfig = Options.Create(new PluginsConfig
        {
            Instances =
            [
                new ServiceInstance { Name = "Plugin1", BaseUrl = new Uri("http://localhost") }
            ]
        });

        var logger = Substitute.For<ILogger<PluginAvailabilityHealthCheck>>();

        var sut = new PluginAvailabilityHealthCheck(clientFactory, pluginConfig, logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_Returns_Unhealthy_When_Plugin_Request_Throws_Exception()
    {
        var clientFactory = Substitute.For<ICreateClient>();

        clientFactory
            .CreateClient(Arg.Any<string>())
            .Returns(_ =>
            {
                var handler = new StubHttpMessageHandler((_, _) => throw new Exception("unexpected"));
                return new HttpClient(handler)
                {
                    BaseAddress = new Uri("http://localhost")
                };
            });

        var pluginConfig = Options.Create(new PluginsConfig
        {
            Instances =
            [
                new ServiceInstance { Name = "Plugin1", BaseUrl = new Uri("http://localhost") }
            ]
        });

        var logger = Substitute.For<ILogger<PluginAvailabilityHealthCheck>>();

        var sut = new PluginAvailabilityHealthCheck(clientFactory, pluginConfig, logger);

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_Checks_All_Plugins_In_Parallel_Even_When_One_Is_Unhealthy()
    {
        var clientFactory = Substitute.For<ICreateClient>();

        clientFactory
            .CreateClient($"{HttpClientNames.PluginHealthCheckPrefix}Plugin1")
            .Returns(CreateHttpClient(HttpStatusCode.InternalServerError));

        clientFactory
            .CreateClient($"{HttpClientNames.PluginHealthCheckPrefix}Plugin2")
            .Returns(CreateHttpClient(HttpStatusCode.OK));

        var pluginConfig = Options.Create(new PluginsConfig
        {
            Instances =
            [
                new ServiceInstance { Name = "Plugin1", BaseUrl = new Uri("http://localhost") },
                new ServiceInstance { Name = "Plugin2", BaseUrl = new Uri("http://localhost") }
            ]
        });

        var logger = Substitute.For<ILogger<PluginAvailabilityHealthCheck>>();

        var sut = new PluginAvailabilityHealthCheck(clientFactory, pluginConfig, logger);

        _ = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        clientFactory.Received(1).CreateClient($"{HttpClientNames.PluginHealthCheckPrefix}Plugin1");
        clientFactory.Received(1).CreateClient($"{HttpClientNames.PluginHealthCheckPrefix}Plugin2");
    }

    [Fact]
    public async Task CheckHealthAsync_Uses_HealthCheck_Client_Names_Without_Retry_Policy()
    {
        var clientFactory = Substitute.For<ICreateClient>();

        clientFactory
            .CreateClient(Arg.Any<string>())
            .Returns(callInfo =>
            {
                return CreateHttpClient(HttpStatusCode.OK);
            });

        var pluginConfig = Options.Create(new PluginsConfig
        {
            Instances =
            [
                new ServiceInstance { Name = "Plugin1", BaseUrl = new Uri("http://localhost") }
            ]
        });

        var logger = Substitute.For<ILogger<PluginAvailabilityHealthCheck>>();

        var sut = new PluginAvailabilityHealthCheck(clientFactory, pluginConfig, logger);

        _ = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        clientFactory.Received(1).CreateClient($"{HttpClientNames.PluginHealthCheckPrefix}Plugin1");
        clientFactory.DidNotReceive().CreateClient($"{HttpClientNames.PluginDataProviderPrefix}Plugin1");
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

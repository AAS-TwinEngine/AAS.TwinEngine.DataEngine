using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Headers;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Extensions;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Http.Extensions;

public class HttpClientRegistrationExtensionsTests
{
    [Fact]
    public void HttpClientRegistrationExtensions_RegistersTemplateProviderClientWithCorrectConfiguration()
    {
        var configValues = new Dictionary<string, string>
            {
                { "HttpRetryPolicyOptions:TemplateProvider:MaxRetryAttempts", "3" },
                { "HttpRetryPolicyOptions:TemplateProvider:DelayInSeconds", "1" }
            };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(configValues!).Build();

        var services = new ServiceCollection();
        services.Configure<HttpRetryPolicyOptions>(HttpRetryPolicyOptions.TemplateProvider, configuration.GetSection($"HttpRetryPolicyOptions:{HttpRetryPolicyOptions.TemplateProvider}"));
        services.AddLogging();
        services.AddHttpContextAccessor();

        var headerMapper = Substitute.For<IRequestHeaderMapper>();
        _ = services.AddScoped(_ => headerMapper);

        services.AddHttpClientWithResilience(configuration, AasEnvironmentConfig.AasEnvironmentRepoHttpClientName, HttpRetryPolicyOptions.TemplateProvider, new Uri("https://example.com"));

        var serviceProvider = services.BuildServiceProvider();

        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName);

        Assert.Contains(httpClient.DefaultRequestHeaders.Accept,
            h => h.MediaType == "application/json");
    }

    [Fact]
    public void HttpClientRegistrationExtensions_RegistersPluginDataProviderClientWithCorrectConfiguration()
    {
        var configValues = new Dictionary<string, string>
            {
                { "HttpRetryPolicyOptions:PluginDataProvider:MaxRetryAttempts", "3" },
                { "HttpRetryPolicyOptions:PluginDataProvider:DelayInSeconds", "1" },
            };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(configValues!).Build();

        var services = new ServiceCollection();
        services.Configure<HttpRetryPolicyOptions>(HttpRetryPolicyOptions.PluginDataProvider, configuration.GetSection($"HttpRetryPolicyOptions:{HttpRetryPolicyOptions.PluginDataProvider}"));
        services.AddLogging();
        services.AddHttpContextAccessor();

        var headerMapper = Substitute.For<IRequestHeaderMapper>();
        _ = services.AddScoped(_ => headerMapper);

        services.AddHttpClientWithResilience(configuration, AasEnvironmentConfig.AasEnvironmentRepoHttpClientName, HttpRetryPolicyOptions.PluginDataProvider, new Uri("https://example.com"));

        var serviceProvider = services.BuildServiceProvider();

        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName);

        Assert.Contains(httpClient.DefaultRequestHeaders.Accept,
            h => h.MediaType == "application/json");
    }

    [Fact]
    public async Task SendRequest_AfterFourAttempts_ThrowsExceptionAndLogsThreeRetries()
    {
        var configValues = new Dictionary<string, string>
            {
                { "HttpRetryPolicyOptions:TemplateProvider:MaxRetryAttempts", "3" },
                { "HttpRetryPolicyOptions:TemplateProvider:DelayInSeconds", "1" },
            };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(configValues!).Build();

        var services = new ServiceCollection();
        services.Configure<HttpRetryPolicyOptions>(HttpRetryPolicyOptions.TemplateProvider, configuration.GetSection($"HttpRetryPolicyOptions:{HttpRetryPolicyOptions.TemplateProvider}"));
        var loggerMock = Substitute.For<ILogger>();
        services.AddSingleton(loggerMock);
        services.AddHttpContextAccessor();

        var headerMapper = Substitute.For<IRequestHeaderMapper>();
        _ = services.AddScoped(_ => headerMapper);

        services.AddHttpClientWithResilience(configuration, AasEnvironmentConfig.AasEnvironmentRepoHttpClientName, HttpRetryPolicyOptions.TemplateProvider, new Uri("https://example.com"));
        using var handler = new FaultyHttpMessageHandler();
        services.Configure<HttpClientFactoryOptions>(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName, options => options.HttpMessageHandlerBuilderActions.Add(builder => builder.PrimaryHandler = handler));
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(new Uri("http://test.com")));

        Assert.Equal(4, handler.CallCount);
    }

    [Fact]
    public async Task AddHttpClientWithResilience_WithForwarding_AddsHeaderForwardingHandler()
    {
        var configValues = new Dictionary<string, string>
            {
                { $"{HttpRetryPolicyOptions.Section}:{HttpRetryPolicyOptions.TemplateProvider}:MaxRetryAttempts", "1" },
                { $"{HttpRetryPolicyOptions.Section}:{HttpRetryPolicyOptions.TemplateProvider}:DelayInSeconds", "1" }
            };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(configValues!).Build();

        var services = new ServiceCollection();
        services.Configure<HttpRetryPolicyOptions>(HttpRetryPolicyOptions.TemplateProvider, configuration.GetSection($"{HttpRetryPolicyOptions.Section}:{HttpRetryPolicyOptions.TemplateProvider}"));
        services.AddLogging();
        services.AddHttpContextAccessor();

        var mappingService = Substitute.For<IRequestHeaderMapper>();
        _ = services.AddScoped(_ => mappingService);

        services.AddHttpClientWithResilience(
            configuration,
            AasEnvironmentConfig.AasEnvironmentRepoHttpClientName,
            HttpRetryPolicyOptions.TemplateProvider,
            new Uri("https://example.com"));

        using var handler = new FaultyHttpMessageHandler();
        services.Configure<HttpClientFactoryOptions>(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName,
            options => options.HttpMessageHandlerBuilderActions.Add(builder => builder.PrimaryHandler = handler));

        var serviceProvider = services.BuildServiceProvider();

        var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        httpContextAccessor.HttpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        var factory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName);

        _ = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync("/test")).ConfigureAwait(false);

        mappingService
            .Received()
            .ApplyMappings(httpContextAccessor.HttpContext, Arg.Any<HttpRequestMessage>(), AasEnvironmentConfig.AasEnvironmentRepoHttpClientName);
    }

    private sealed class FaultyHttpMessageHandler : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            throw new HttpRequestException("Simulated failure");
        }
    }
}

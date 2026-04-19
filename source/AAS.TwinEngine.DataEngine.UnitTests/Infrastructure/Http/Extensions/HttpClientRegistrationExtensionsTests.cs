using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Headers;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Extensions;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Http.Extensions;

public class HttpClientRegistrationExtensionsTests
{
    private static readonly RetryConfig DefaultRetryConfig = new() { MaxRetryAttempts = 3, DelayInSeconds = 1 };

    [Fact]
    public void HttpClientRegistrationExtensions_RegistersTemplateProviderClientWithCorrectConfiguration()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();

        var headerMapper = Substitute.For<IRequestHeaderMapper>();
        _ = services.AddScoped(_ => headerMapper);

        services.AddHttpClientWithResilience(HttpClientNames.SubmodelTemplateRepository, DefaultRetryConfig, new Uri("https://example.com"));

        var serviceProvider = services.BuildServiceProvider();

        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient(HttpClientNames.SubmodelTemplateRepository);

        Assert.Contains(httpClient.DefaultRequestHeaders.Accept,
            h => h.MediaType == "application/json");
    }

    [Fact]
    public void HttpClientRegistrationExtensions_RegistersPluginDataProviderClientWithCorrectConfiguration()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();

        var headerMapper = Substitute.For<IRequestHeaderMapper>();
        _ = services.AddScoped(_ => headerMapper);

        services.AddHttpClientWithResilience(HttpClientNames.SubmodelTemplateRepository, DefaultRetryConfig, new Uri("https://example.com"));

        var serviceProvider = services.BuildServiceProvider();

        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient(HttpClientNames.SubmodelTemplateRepository);

        Assert.Contains(httpClient.DefaultRequestHeaders.Accept,
            h => h.MediaType == "application/json");
    }

    [Fact]
    public async Task SendRequest_AfterFourAttempts_ThrowsExceptionAndLogsThreeRetries()
    {
        var services = new ServiceCollection();
        var loggerMock = Substitute.For<ILogger>();
        services.AddSingleton(loggerMock);
        services.AddHttpContextAccessor();

        var headerMapper = Substitute.For<IRequestHeaderMapper>();
        _ = services.AddScoped(_ => headerMapper);

        services.AddHttpClientWithResilience(HttpClientNames.SubmodelTemplateRepository, DefaultRetryConfig, new Uri("https://example.com"));
        using var handler = new FaultyHttpMessageHandler();
        services.Configure<HttpClientFactoryOptions>(HttpClientNames.SubmodelTemplateRepository, options => options.HttpMessageHandlerBuilderActions.Add(builder => builder.PrimaryHandler = handler));
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient(HttpClientNames.SubmodelTemplateRepository);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(new Uri("http://test.com")));

        Assert.Equal(4, handler.CallCount);
    }

    [Fact]
    public async Task AddHttpClientWithResilience_WithForwarding_AddsHeaderForwardingHandler()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();

        var mappingService = Substitute.For<IRequestHeaderMapper>();
        _ = services.AddScoped(_ => mappingService);

        services.AddHttpClientWithResilience(
            HttpClientNames.SubmodelTemplateRepository,
            DefaultRetryConfig,
            new Uri("https://example.com"));

        using var handler = new FaultyHttpMessageHandler();
        services.Configure<HttpClientFactoryOptions>(HttpClientNames.SubmodelTemplateRepository,
            options => options.HttpMessageHandlerBuilderActions.Add(builder => builder.PrimaryHandler = handler));

        var serviceProvider = services.BuildServiceProvider();

        var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        httpContextAccessor.HttpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        var factory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient(HttpClientNames.SubmodelTemplateRepository);

        _ = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync("/test")).ConfigureAwait(false);

        mappingService
            .Received()
            .ApplyMappings(httpContextAccessor.HttpContext, Arg.Any<HttpRequestMessage>(), HttpClientNames.SubmodelTemplateRepository);
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

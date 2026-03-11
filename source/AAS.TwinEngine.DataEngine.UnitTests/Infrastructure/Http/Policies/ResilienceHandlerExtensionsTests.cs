using System.Net;

using AAS.TwinEngine.DataEngine.Infrastructure.Http.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Policies;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Http.Policies;

public class ResilienceHandlerExtensionsTests
{
    private const string ClientName = "TestClient";

    [Fact]
    public async Task AddStandardResilienceHandler_RetriesHttpRequestException()
    {
        var services = CreateServiceCollection(maxRetries: 2, delaySeconds: 1);
        var handler = new ExceptionThrowingHttpMessageHandler(new HttpRequestException("Network error"));
        ConfigureHandler(services, handler);

        var client = CreateHttpClient(services);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync("/test"));

        Assert.Equal(3, handler.CallCount);
    }

    [Fact]
    public async Task AddStandardResilienceHandler_RetriesTimeoutException()
    {
        var services = CreateServiceCollection(maxRetries: 2, delaySeconds: 1);
        var handler = new ExceptionThrowingHttpMessageHandler(new TimeoutException("Request timeout"));
        ConfigureHandler(services, handler);

        var client = CreateHttpClient(services);

        await Assert.ThrowsAsync<TimeoutException>(() => client.GetAsync("/test"));

        Assert.Equal(3, handler.CallCount);
    }

    [Fact]
    public async Task AddStandardResilienceHandler_RespectsMaxRetryAttempts()
    {
        var services = CreateServiceCollection(maxRetries: 5, delaySeconds: 1);
        var handler = new ExceptionThrowingHttpMessageHandler(new HttpRequestException("Network error"));
        ConfigureHandler(services, handler);

        var client = CreateHttpClient(services);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync("/test"));

        Assert.Equal(6, handler.CallCount);
    }

    [Fact]
    public async Task AddStandardResilienceHandler_UsesExponentialBackoff()
    {
        var services = CreateServiceCollection(maxRetries: 3, delaySeconds: 1);
        var handler = new ExceptionThrowingHttpMessageHandler(new HttpRequestException("Network error"));
        ConfigureHandler(services, handler);

        var client = CreateHttpClient(services);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync("/test"));

        Assert.Equal(4, handler.CallCount);
    }

    private static ServiceCollection CreateServiceCollection(int maxRetries, int delaySeconds)
    {
        var configValues = new Dictionary<string, string>
        {
            { $"{HttpRetryPolicyOptions.Section}:{ClientName}:MaxRetryAttempts", maxRetries.ToString() },
            { $"{HttpRetryPolicyOptions.Section}:{ClientName}:DelayInSeconds", delaySeconds.ToString() }
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues!)
            .Build();

        var services = new ServiceCollection();
        services.Configure<HttpRetryPolicyOptions>(ClientName, configuration.GetSection($"{HttpRetryPolicyOptions.Section}:{ClientName}"));
        services.AddLogging();

        services.AddHttpClient(ClientName, client =>
        {
            client.BaseAddress = new Uri("https://example.com");
        })
        .AddStandardResilienceHandler(ClientName);

        return services;
    }

    private static void ConfigureHandler(ServiceCollection services, HttpMessageHandler handler)
    {
        services.Configure<HttpClientFactoryOptions>(ClientName, options =>
        {
            options.HttpMessageHandlerBuilderActions.Add(builder => builder.PrimaryHandler = handler);
        });
    }

    private static HttpClient CreateHttpClient(ServiceCollection services)
    {
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        return factory.CreateClient(ClientName);
    }

    private sealed class ExceptionThrowingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Exception _exception;
        public int CallCount { get; private set; }

        public ExceptionThrowingHttpMessageHandler(Exception exception)
        {
            _exception = exception;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            throw _exception;
        }
    }
}

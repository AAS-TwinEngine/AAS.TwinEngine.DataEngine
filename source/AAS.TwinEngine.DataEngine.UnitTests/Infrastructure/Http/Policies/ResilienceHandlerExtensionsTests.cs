using System.Net;

using AAS.TwinEngine.DataEngine.Infrastructure.Http.Policies;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

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
    public async Task AddStandardResilienceHandler_RetriesHttp5xxErrors()
    {
        var services = CreateServiceCollection(maxRetries: 2, delaySeconds: 1);
        var handler = new StatusCodeReturningHttpMessageHandler(HttpStatusCode.InternalServerError);
        ConfigureHandler(services, handler);

        var client = CreateHttpClient(services);

        var response = await client.GetAsync("/test");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(3, handler.CallCount);
    }

    [Fact]
    public async Task AddStandardResilienceHandler_RetriesHttp408RequestTimeout()
    {
        var services = CreateServiceCollection(maxRetries: 2, delaySeconds: 1);
        var handler = new StatusCodeReturningHttpMessageHandler(HttpStatusCode.RequestTimeout);
        ConfigureHandler(services, handler);

        var client = CreateHttpClient(services);

        var response = await client.GetAsync("/test");

        Assert.Equal(HttpStatusCode.RequestTimeout, response.StatusCode);
        Assert.Equal(3, handler.CallCount);
    }

    [Fact]
    public async Task AddStandardResilienceHandler_DoesNotRetryHttp4xxClientErrors()
    {
        var services = CreateServiceCollection(maxRetries: 2, delaySeconds: 1);
        var handler = new StatusCodeReturningHttpMessageHandler(HttpStatusCode.NotFound);
        ConfigureHandler(services, handler);

        var client = CreateHttpClient(services);

        var response = await client.GetAsync("/test");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task AddStandardResilienceHandler_DoesNotRetryHttp401Unauthorized()
    {
        var services = CreateServiceCollection(maxRetries: 2, delaySeconds: 1);
        var handler = new StatusCodeReturningHttpMessageHandler(HttpStatusCode.Unauthorized);
        ConfigureHandler(services, handler);

        var client = CreateHttpClient(services);

        var response = await client.GetAsync("/test");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(1, handler.CallCount);
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
        var retryConfig = new RetryConfig
        {
            MaxRetryAttempts = maxRetries,
            DelayInSeconds = delaySeconds
        };

        var services = new ServiceCollection();
        services.AddLogging();

        services.AddHttpClient(ClientName, client =>
        {
            client.BaseAddress = new Uri("https://example.com");
        })
        .AddStandardResilienceHandler(retryConfig);

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

    private sealed class StatusCodeReturningHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        public int CallCount { get; private set; }

        public StatusCodeReturningHttpMessageHandler(HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new HttpResponseMessage(_statusCode));
        }
    }
}

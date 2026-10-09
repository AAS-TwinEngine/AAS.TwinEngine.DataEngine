using System.Net;

using AAS.TwinEngine.ExportService.Infrastructure.Http.Policies;
using AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AAS.TwinEngine.ExportService.UnitTests.Infrastructure.Http.Policies;

public class ResilienceHandlerExtensionsTests
{
    private sealed class FlakyHandler : HttpMessageHandler
    {
        private int _attempts;
        public int Attempts => _attempts;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _attempts++;
            if (_attempts < 3)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    [Fact]
    public async Task AddStandardResilience_AppliesRetryOnTransientFailures()
    {
        // Arrange
        var services = new ServiceCollection();
        _ = services.AddLogging(builder => builder.AddConsole());

        var config = new ResilienceConfig
        {
            MaxRetryAttempts = 3,
            InitialDelaySeconds = 0
        };

        var flakyHandler = new FlakyHandler();
        _ = services.AddHttpClient("resilient-client")
            .ConfigurePrimaryHttpMessageHandler(() => flakyHandler)
            .AddStandardResilience(config);

        var provider = services.BuildServiceProvider();
        var clientFactory = provider.GetRequiredService<IHttpClientFactory>();
        var client = clientFactory.CreateClient("resilient-client");

        // Act
        var response = await client.GetAsync(new Uri("http://localhost/test"), CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, flakyHandler.Attempts);
    }
}

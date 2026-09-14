using System.Net;

using AAS.TwinEngine.ExportService.DomainModel;
using AAS.TwinEngine.ExportService.Infrastructure.Http.Clients;
using AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace AAS.TwinEngine.ExportService.UnitTests.Infrastructure.Http.Clients;

public class TargetEntityHttpClientTests
{
    private readonly IHttpClientFactory _httpClientFactory = Substitute.For<IHttpClientFactory>();
    private readonly IOptionsMonitor<ExportServiceConfig> _config = Substitute.For<IOptionsMonitor<ExportServiceConfig>>();
    private readonly ILogger<TargetEntityHttpClient> _logger = Substitute.For<ILogger<TargetEntityHttpClient>>();
    private readonly ExportServiceConfig _configValue = new();

    public TargetEntityHttpClientTests()
    {
        _config.CurrentValue.Returns(_configValue);
        _configValue.Targets.Shells = new EndpointConfig
        {
            Enabled = true,
            BaseUrl = "http://localhost:8080",
            Path = "/api/v3.0/shells"
        };
    }

    private sealed class DelegatingTestHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;
        public List<HttpRequestMessage> Requests { get; } = new();
        public List<string> RequestBodies { get; } = new();

        public DelegatingTestHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
            => _handler = handler;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            if (request.Content is not null)
            {
                RequestBodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));
            }
            return _handler(request);
        }
    }

    private TargetEntityHttpClient CreateSut(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8080") };
        _httpClientFactory.CreateClient(Arg.Any<string>()).Returns(client);
        return new TargetEntityHttpClient(_httpClientFactory, _config, _logger);
    }

    [Fact]
    public async Task CreateAsync_WhenTargetReturnsOk_SendsPostRequestWithJsonContent()
    {
        // Arrange
        var handler = new DelegatingTestHandler(req => new HttpResponseMessage(HttpStatusCode.Created));
        var sut = CreateSut(handler);
        var entity = new SourceEntity("shell-123", "{\"id\":\"shell-123\"}");

        // Act
        await sut.CreateAsync(EntityKind.Shell, entity, CancellationToken.None);

        // Assert
        Assert.Single(handler.Requests);
        var req = handler.Requests[0];
        Assert.Equal(HttpMethod.Post, req.Method);
        Assert.Equal("/api/v3.0/shells", req.RequestUri?.AbsolutePath);
        Assert.Equal("{\"id\":\"shell-123\"}", handler.RequestBodies[0]);
    }

    [Fact]
    public async Task CreateAsync_WhenTargetReturnsConflict_FallsBackToUpdate()
    {
        // Arrange
        var handler = new DelegatingTestHandler(req =>
        {
            if (req.Method == HttpMethod.Post)
            {
                return new HttpResponseMessage(HttpStatusCode.Conflict);
            }
            if (req.Method == HttpMethod.Put)
            {
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }
            return new HttpResponseMessage(HttpStatusCode.BadRequest);
        });

        var sut = CreateSut(handler);
        var entity = new SourceEntity("shell-123", "{\"id\":\"shell-123\"}");

        // Act
        await sut.CreateAsync(EntityKind.Shell, entity, CancellationToken.None);

        // Assert
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal(HttpMethod.Put, handler.Requests[1].Method);
        Assert.Contains(Base64Url.Encode("shell-123"), handler.Requests[1].RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task CreateAsync_WhenTargetReturnsError_ThrowsHttpRequestException()
    {
        // Arrange
        var handler = new DelegatingTestHandler(req => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var sut = CreateSut(handler);
        var entity = new SourceEntity("shell-123", "{\"id\":\"shell-123\"}");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() =>
            sut.CreateAsync(EntityKind.Shell, entity, CancellationToken.None));
        Assert.Contains("500", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_WhenTargetReturnsOk_SendsPutRequestWithBase64UrlEncodedIdentifier()
    {
        // Arrange
        var handler = new DelegatingTestHandler(req => new HttpResponseMessage(HttpStatusCode.NoContent));
        var sut = CreateSut(handler);
        const string rawId = "urn:test:shell:001";
        var entity = new SourceEntity(rawId, "{\"id\":\"urn:test:shell:001\"}");

        // Act
        await sut.UpdateAsync(EntityKind.Shell, entity, CancellationToken.None);

        // Assert
        Assert.Single(handler.Requests);
        var req = handler.Requests[0];
        Assert.Equal(HttpMethod.Put, req.Method);
        var expectedPath = $"/api/v3.0/shells/{Base64Url.Encode(rawId)}";
        Assert.Equal(expectedPath, req.RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task UpdateAsync_WhenTargetReturnsError_ThrowsHttpRequestException()
    {
        // Arrange
        var handler = new DelegatingTestHandler(req => new HttpResponseMessage(HttpStatusCode.BadRequest));
        var sut = CreateSut(handler);
        var entity = new SourceEntity("shell-123", "{\"id\":\"shell-123\"}");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() =>
            sut.UpdateAsync(EntityKind.Shell, entity, CancellationToken.None));
        Assert.Contains("400", ex.Message);
    }

    [Fact]
    public async Task DeleteAsync_WhenTargetReturnsOk_SendsDeleteRequestWithBase64UrlEncodedIdentifier()
    {
        // Arrange
        var handler = new DelegatingTestHandler(req => new HttpResponseMessage(HttpStatusCode.NoContent));
        var sut = CreateSut(handler);
        const string rawId = "urn:test:shell:001";

        // Act
        await sut.DeleteAsync(EntityKind.Shell, rawId, CancellationToken.None);

        // Assert
        Assert.Single(handler.Requests);
        var req = handler.Requests[0];
        Assert.Equal(HttpMethod.Delete, req.Method);
        var expectedPath = $"/api/v3.0/shells/{Base64Url.Encode(rawId)}";
        Assert.Equal(expectedPath, req.RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task DeleteAsync_WhenTargetReturnsNotFound_TreatsAsSuccessWithoutThrowing()
    {
        // Arrange
        var handler = new DelegatingTestHandler(req => new HttpResponseMessage(HttpStatusCode.NotFound));
        var sut = CreateSut(handler);

        // Act & Assert (does not throw)
        await sut.DeleteAsync(EntityKind.Shell, "already-deleted-id", CancellationToken.None);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task DeleteAsync_WhenTargetReturnsServerError_ThrowsHttpRequestException()
    {
        // Arrange
        var handler = new DelegatingTestHandler(req => new HttpResponseMessage(HttpStatusCode.BadGateway));
        var sut = CreateSut(handler);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() =>
            sut.DeleteAsync(EntityKind.Shell, "shell-123", CancellationToken.None));
        Assert.Contains("502", ex.Message);
    }
}

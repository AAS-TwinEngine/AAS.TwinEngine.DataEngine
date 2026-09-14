using System.Net;

using AAS.TwinEngine.ExportService.ApplicationLogic.Exceptions;
using AAS.TwinEngine.ExportService.DomainModel;
using AAS.TwinEngine.ExportService.Infrastructure.Http.Clients;
using AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace AAS.TwinEngine.ExportService.UnitTests.Infrastructure.Http.Clients;

public class SourceEntityHttpClientTests
{
    private readonly IHttpClientFactory _httpClientFactory = Substitute.For<IHttpClientFactory>();
    private readonly IOptionsMonitor<ExportServiceConfig> _config = Substitute.For<IOptionsMonitor<ExportServiceConfig>>();
    private readonly ILogger<SourceEntityHttpClient> _logger = Substitute.For<ILogger<SourceEntityHttpClient>>();
    private readonly ExportServiceConfig _configValue = new();

    public SourceEntityHttpClientTests()
    {
        _config.CurrentValue.Returns(_configValue);
        _configValue.Sources.Shells = new EndpointConfig
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

        public DelegatingTestHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
            => _handler = handler;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(_handler(request));
        }
    }

    private SourceEntityHttpClient CreateSut(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8080") };
        _httpClientFactory.CreateClient(Arg.Any<string>()).Returns(client);
        return new SourceEntityHttpClient(_httpClientFactory, _config, _logger);
    }

    [Fact]
    public async Task ReadAllAsync_WhenEndpointDisabled_ReturnsEmptyListWithoutHttpCall()
    {
        // Arrange
        _configValue.Sources.Shells.Enabled = false;
        var handler = new DelegatingTestHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var sut = CreateSut(handler);

        // Act
        var result = await sut.ReadAllAsync(EntityKind.Shell, CancellationToken.None);

        // Assert
        Assert.Empty(result);
        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ReadAllAsync_WhenEndpointPathIsMissing_ThrowsSourceUnavailableException(string? path)
    {
        // Arrange
        _configValue.Sources.Shells.Path = path!;
        var handler = new DelegatingTestHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var sut = CreateSut(handler);

        // Act & Assert
        await Assert.ThrowsAsync<SourceUnavailableException>(() =>
            sut.ReadAllAsync(EntityKind.Shell, CancellationToken.None));
    }

    [Fact]
    public async Task ReadAllAsync_WhenPlainJsonArrayWithId_ParsesEntities()
    {
        // Arrange
        const string json = "[{\"id\":\"shell-1\",\"name\":\"first\"},{\"id\":\"shell-2\",\"name\":\"second\"}]";
        var handler = new DelegatingTestHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        });
        var sut = CreateSut(handler);

        // Act
        var result = await sut.ReadAllAsync(EntityKind.Shell, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("shell-1", result[0].Identifier);
        Assert.Equal("{\"id\":\"shell-1\",\"name\":\"first\"}", result[0].RawJson);
        Assert.Equal("shell-2", result[1].Identifier);
        Assert.Equal("{\"id\":\"shell-2\",\"name\":\"second\"}", result[1].RawJson);
    }

    [Fact]
    public async Task ReadAllAsync_WhenPlainJsonArrayWithIdentificationId_ParsesEntities()
    {
        // Arrange
        const string json = "[{\"identification\":{\"id\":\"urn:shell:legacy\"}}]";
        var handler = new DelegatingTestHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        });
        var sut = CreateSut(handler);

        // Act
        var result = await sut.ReadAllAsync(EntityKind.Shell, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("urn:shell:legacy", result[0].Identifier);
    }

    [Fact]
    public async Task ReadAllAsync_WhenPagedResponse_FollowsCursorUntilExhausted()
    {
        // Arrange
        var handler = new DelegatingTestHandler(req =>
        {
            var uri = req.RequestUri?.ToString() ?? string.Empty;
            if (!uri.Contains("cursor=", StringComparison.Ordinal))
            {
                // Page 1
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"paging_metadata\":{\"cursor\":\"page2_token\"},\"result\":[{\"id\":\"e1\"}]}")
                };
            }
            if (uri.Contains("cursor=page2_token", StringComparison.Ordinal))
            {
                // Page 2 (last)
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"paging_metadata\":{\"cursor\":\"\"},\"result\":[{\"id\":\"e2\"}]}")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
        var sut = CreateSut(handler);

        // Act
        var result = await sut.ReadAllAsync(EntityKind.Shell, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("e1", result[0].Identifier);
        Assert.Equal("e2", result[1].Identifier);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task ReadAllAsync_WhenPathContainsQueryParam_UsesAmpersandForCursor()
    {
        // Arrange
        _configValue.Sources.Shells.Path = "/api/v3.0/shells?filter=active";
        var handler = new DelegatingTestHandler(req =>
        {
            var uri = req.RequestUri?.ToString() ?? string.Empty;
            if (!uri.Contains("cursor=", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"paging_metadata\":{\"cursor\":\"c1\"},\"result\":[{\"id\":\"e1\"}]}")
                };
            }

            Assert.Contains("?filter=active&cursor=c1", uri);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"paging_metadata\":{\"cursor\":null},\"result\":[]}")
            };
        });
        var sut = CreateSut(handler);

        // Act
        var result = await sut.ReadAllAsync(EntityKind.Shell, CancellationToken.None);

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task ReadAllAsync_WhenRepeatedCursorEncountered_BreaksLoopToPreventHang()
    {
        // Arrange
        var handler = new DelegatingTestHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"paging_metadata\":{\"cursor\":\"infinite_loop\"},\"result\":[{\"id\":\"e1\"}]}")
        });
        var sut = CreateSut(handler);

        // Act
        var result = await sut.ReadAllAsync(EntityKind.Shell, CancellationToken.None);

        // Assert - Stops after second request where cursor repeats
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ReadAllAsync_WhenResponseNotArrayOrPagedWrapper_ThrowsSourceUnavailableException()
    {
        // Arrange
        var handler = new DelegatingTestHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"unexpected\":\"object\"}")
        });
        var sut = CreateSut(handler);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<SourceUnavailableException>(() =>
            sut.ReadAllAsync(EntityKind.Shell, CancellationToken.None));
        Assert.Contains("not a JSON array", ex.Message);
    }

    [Fact]
    public async Task ReadAllAsync_WhenEntityMissingId_ThrowsSourceUnavailableException()
    {
        // Arrange
        var handler = new DelegatingTestHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[{\"no_id_here\":\"value\"}]")
        });
        var sut = CreateSut(handler);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<SourceUnavailableException>(() =>
            sut.ReadAllAsync(EntityKind.Shell, CancellationToken.None));
        Assert.Contains("missing its 'id'", ex.Message);
    }

    [Fact]
    public async Task ReadAllAsync_WhenHttpReturnsNonSuccessStatus_ThrowsSourceUnavailableException()
    {
        // Arrange
        var handler = new DelegatingTestHandler(req => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var sut = CreateSut(handler);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<SourceUnavailableException>(() =>
            sut.ReadAllAsync(EntityKind.Shell, CancellationToken.None));
        Assert.Contains("500", ex.Message);
    }

    [Fact]
    public async Task ReadAllAsync_WhenOperationCanceledExceptionThrown_RethrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var handler = new DelegatingTestHandler(req => throw new OperationCanceledException(cts.Token));
        var sut = CreateSut(handler);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            sut.ReadAllAsync(EntityKind.Shell, CancellationToken.None));
    }

    [Fact]
    public async Task ReadAllAsync_WhenGenericExceptionThrown_WrapsInSourceUnavailableException()
    {
        // Arrange
        var handler = new DelegatingTestHandler(req => throw new InvalidOperationException("Network reset"));
        var sut = CreateSut(handler);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<SourceUnavailableException>(() =>
            sut.ReadAllAsync(EntityKind.Shell, CancellationToken.None));
        Assert.Contains("Network reset", ex.Message);
    }

    [Fact]
    public async Task ReadAllAsync_WhenArrayElementIsNotJsonObject_ThrowsSourceUnavailableException()
    {
        // Arrange
        var handler = new DelegatingTestHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[123]")
        });
        var sut = CreateSut(handler);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<SourceUnavailableException>(() =>
            sut.ReadAllAsync(EntityKind.Shell, CancellationToken.None));
        Assert.Contains("missing its 'id'", ex.Message);
    }

    [Fact]
    public async Task ReadAllAsync_WhenPagingMetadataIsNotAnObject_TreatsNextCursorAsNull()
    {
        // Arrange
        var handler = new DelegatingTestHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"paging_metadata\":\"not_an_object\",\"result\":[{\"id\":\"e1\"}]}")
        });
        var sut = CreateSut(handler);

        // Act
        var result = await sut.ReadAllAsync(EntityKind.Shell, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("e1", result[0].Identifier);
    }
}

using System.Net;
using System.Text;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.FileContentProvider.Services;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Streaming;

public class FileContentProviderTests
{
    private readonly ILogger<FileContentProvider> _logger = Substitute.For<ILogger<FileContentProvider>>();

    [Fact]
    public async Task GetFileContentAsync_ReturnsStreamAndContentType()
    {
        var expectedUrl = "https://example.com/file.bin";
        var payload = "file-content";

        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal(expectedUrl, request.RequestUri?.ToString());
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/octet-stream")
            };
            response.Content.Headers.ContentLength = Encoding.UTF8.GetByteCount(payload);
            return Task.FromResult(response);
        });

        using var client = new HttpClient(handler);
        var httpClientFactory = Substitute.For<ICreateClient>();
        httpClientFactory.CreateClient(HttpClientNames.FileAttachmentProvider).Returns(client);

        var options = Options.Create(new GeneralConfig { MaxFileAttachmentSizeBytes = 100 * 1024 * 1024 });
        var sut = new FileContentProvider(httpClientFactory, options, _logger);

        var result = await sut.GetFileContentAsync(expectedUrl, CancellationToken.None);
        await using (result)
        {
            using var reader = new StreamReader(result.Content, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();
            Assert.Equal(payload, body);
        }

        _ = httpClientFactory.Received(1).CreateClient(HttpClientNames.FileAttachmentProvider);
    }

    [Fact]
    public async Task GetFileContentAsync_WhenDisposed_DisposesUnderlyingHttpResponse()
    {
        var expectedUrl = "https://example.com/file.bin";
        using var response = CreateTrackingResponse(HttpStatusCode.OK, "content");

        using var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult<HttpResponseMessage>(response));
        using var client = new HttpClient(handler);
        var httpClientFactory = Substitute.For<ICreateClient>();
        httpClientFactory.CreateClient(HttpClientNames.FileAttachmentProvider).Returns(client);

        var options = Options.Create(new GeneralConfig { MaxFileAttachmentSizeBytes = 100 * 1024 * 1024 });
        var sut = new FileContentProvider(httpClientFactory, options, _logger);

        var result = await sut.GetFileContentAsync(expectedUrl, CancellationToken.None);
        Assert.False(response.IsDisposed);

        await result.DisposeAsync();

        Assert.True(response.IsDisposed);
    }

    [Fact]
    public async Task GetFileContentAsync_WhenResponseIsNotSuccess_ThrowsInternalDataProcessingExceptionAndLogsError()
    {
        var expectedUrl = "https://example.com/not-found.bin";

        using var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(string.Empty)
            }));

        using var client = new HttpClient(handler);
        var httpClientFactory = Substitute.For<ICreateClient>();
        httpClientFactory.CreateClient(HttpClientNames.FileAttachmentProvider).Returns(client);

        var options = Options.Create(new GeneralConfig { MaxFileAttachmentSizeBytes = 100 * 1024 * 1024 });
        var sut = new FileContentProvider(httpClientFactory, options, _logger);

        await Assert.ThrowsAsync<InternalDataProcessingException>(() =>
            sut.GetFileContentAsync(expectedUrl, CancellationToken.None));

        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(state =>
                state.ToString()!.Contains("Failed to retrieve file content") &&
                state.ToString()!.Contains(expectedUrl) &&
                state.ToString()!.Contains(HttpStatusCode.NotFound.ToString())),
            null,
            Arg.Any<Func<object, Exception?, string>>()!);
    }

    [Fact]
    public async Task GetFileContentAsync_WhenContentLengthExceedsMax_ThrowsFileSizeExceededException()
    {
        var expectedUrl = "https://example.com/large-file.bin";
        using var response = CreateTrackingResponse(HttpStatusCode.OK, "x");
        response.Content.Headers.ContentLength = 200 * 1024 * 1024;

        using var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult<HttpResponseMessage>(response));

        using var client = new HttpClient(handler);
        var httpClientFactory = Substitute.For<ICreateClient>();
        httpClientFactory.CreateClient(HttpClientNames.FileAttachmentProvider).Returns(client);

        var options = Options.Create(new GeneralConfig { MaxFileAttachmentSizeBytes = 100 });
        var sut = new FileContentProvider(httpClientFactory, options, _logger);

        await Assert.ThrowsAsync<FileSizeExceededException>(() =>
            sut.GetFileContentAsync(expectedUrl, CancellationToken.None));

        Assert.True(response.IsDisposed);
    }

    private static TrackingHttpResponseMessage CreateTrackingResponse(HttpStatusCode statusCode, string body)
        => new(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/octet-stream")
        };

    private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => send(request, cancellationToken);
    }

    private sealed class TrackingHttpResponseMessage(HttpStatusCode statusCode) : HttpResponseMessage(statusCode)
    {
        public bool IsDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }
}

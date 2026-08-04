using System.Net;
using System.Text;

using AAS.TwinEngine.DataEngine.Infrastructure.Streaming;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Streaming;

public class FileContentProviderTests
{
    [Fact]
    public async Task GetResponseHeadersAsync_UsesNamedClientAndReturnsResponse()
    {
        var expectedUrl = "https://example.com/file.bin";
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("ok", Encoding.UTF8, "text/plain")
        };

        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal(expectedUrl, request.RequestUri?.ToString());
            return Task.FromResult(expectedResponse);
        });

        using var client = new HttpClient(handler);
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(HttpClientNames.FileAttachmentProvider).Returns(client);

        var sut = new FileContentProvider(httpClientFactory);

        var result = await sut.GetResponseHeadersAsync(expectedUrl, CancellationToken.None);

        Assert.Same(expectedResponse, result);
        _ = httpClientFactory.Received(1).CreateClient(HttpClientNames.FileAttachmentProvider);
    }

    [Fact]
    public async Task ReadStreamAsync_ReturnsContentStream()
    {
        var payload = "stream-content";
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "text/plain")
        };

        var sut = new FileContentProvider(Substitute.For<IHttpClientFactory>());

        await using var stream = await sut.ReadStreamAsync(response, CancellationToken.None);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: false);
        var body = await reader.ReadToEndAsync();

        Assert.Equal(payload, body);
    }

    private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => send(request, cancellationToken);
    }
}

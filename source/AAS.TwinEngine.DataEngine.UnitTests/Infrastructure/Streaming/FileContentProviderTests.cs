using System.Net;
using System.Text;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.FileContentProvider.Services;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.Extensions.Options;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Streaming;

public class FileContentProviderTests
{
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
        var sut = new FileContentProvider(httpClientFactory, options);

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
    public async Task GetFileContentAsync_WhenContentLengthExceedsMax_ThrowsFileSizeExceededException()
    {
        var expectedUrl = "https://example.com/large-file.bin";

        using var handler = new FakeHttpMessageHandler((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("x", Encoding.UTF8, "application/octet-stream")
            };
            response.Content.Headers.ContentLength = 200 * 1024 * 1024;
            return Task.FromResult(response);
        });

        using var client = new HttpClient(handler);
        var httpClientFactory = Substitute.For<ICreateClient>();
        httpClientFactory.CreateClient(HttpClientNames.FileAttachmentProvider).Returns(client);

        var options = Options.Create(new GeneralConfig { MaxFileAttachmentSizeBytes = 100 });
        var sut = new FileContentProvider(httpClientFactory, options);

        await Assert.ThrowsAsync<FileSizeExceededException>(() =>
            sut.GetFileContentAsync(expectedUrl, CancellationToken.None));
    }

    private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => send(request, cancellationToken);
    }
}

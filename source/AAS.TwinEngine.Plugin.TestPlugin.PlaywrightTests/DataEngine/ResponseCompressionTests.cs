using Microsoft.Playwright;

namespace AAS.TwinEngine.Plugin.TestPlugin.PlaywrightTests.DataEngine;

/// <summary>
/// Tests for Data Engine response compression behavior
/// </summary>
public class ResponseCompressionTests : ApiTestBase
{
    [Fact]
    public async Task GetSwaggerJson_WhenClientRequestsGzip_ReturnsGzipEncodingHeader()
    {
        // Arrange
        var url = "/swagger/1.0/swagger.json";

        // Act
        var response = await ApiContext.GetAsync(url, new()
        {
            Headers = new Dictionary<string, string>
            {
                { "Accept-Encoding", "gzip" }
            }
        });

        // Assert
        AssertSuccessResponse(response);
        var contentEncoding = GetHeaderValue(response, "content-encoding");
        var vary = GetHeaderValue(response, "vary");

        Assert.NotNull(contentEncoding);
        Assert.Contains("gzip", contentEncoding, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(vary);
        Assert.Contains("accept-encoding", vary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetSwaggerJson_WhenClientRequestsIdentity_DoesNotReturnCompressedEncodingHeader()
    {
        // Arrange
        var url = "/swagger/1.0/swagger.json";

        // Act
        var response = await ApiContext.GetAsync(url, new()
        {
            Headers = new Dictionary<string, string>
            {
                { "Accept-Encoding", "identity" }
            }
        });

        // Assert
        AssertSuccessResponse(response);
        var contentEncoding = GetHeaderValue(response, "content-encoding");
        Assert.True(string.IsNullOrWhiteSpace(contentEncoding));
    }

    [Fact]
    public async Task GetSwaggerJson_WhenClientRequestsBothBrAndGzip_ReturnsBrotliCompressedResponse()
    {
        // Arrange
        var url = "/swagger/1.0/swagger.json";

        // Act
        var response = await ApiContext.GetAsync(url, new()
        {
            Headers = new Dictionary<string, string>
            {
                { "Accept-Encoding", "br, gzip" }
            }
        });

        // Assert
        // Brotli is the first registered provider in ConfigureResponseCompression,
        // so DataEngine should prefer it when the client accepts both.
        AssertSuccessResponse(response);
        var contentEncoding = GetHeaderValue(response, "content-encoding");
        var vary = GetHeaderValue(response, "vary");

        Assert.NotNull(contentEncoding);
        Assert.Contains("br", contentEncoding, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(vary);
        Assert.Contains("accept-encoding", vary, StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetHeaderValue(IAPIResponse response, string headerName)
    {
        foreach (var header in response.Headers)
        {
            if (string.Equals(header.Key, headerName, StringComparison.OrdinalIgnoreCase))
            {
                return header.Value;
            }
        }

        return null;
    }
}

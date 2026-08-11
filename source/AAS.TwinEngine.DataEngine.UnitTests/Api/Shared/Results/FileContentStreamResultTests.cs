using AAS.TwinEngine.DataEngine.Api.Shared.Results;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AAS.TwinEngine.DataEngine.UnitTests.Api.Shared.Results;

public class FileContentStreamResultTests
{
    private const long MaxFileSize = 100 * 1024 * 1024; // 100 MB

    [Fact]
    public async Task ExecuteResultAsync_WithAttachmentDisposition_SetsContentDispositionHeaderWithFilename()
    {
        // Arrange
        const string fileName = "document.pdf";
        const string contentType = "application/pdf";
        var fileContent = "PDF Content"u8.ToArray();
        var stream = new MemoryStream(fileContent);
        var attachment = new FileAttachmentResult(stream, contentType, fileName, MaxFileSize);
        var result = new FileContentStreamResult(attachment, ContentDispositionType.attachment);

        var httpContext = CreateMockHttpContext();
        var actionContext = new ActionContext(httpContext, new(), new(), new());

        // Act
        await result.ExecuteResultAsync(actionContext);

        // Assert
        var dispositionHeader = httpContext.Response.Headers["Content-Disposition"].ToString();
        Assert.Contains("attachment", dispositionHeader);
        Assert.Contains($"filename=\"{fileName}\"", dispositionHeader);
    }

    [Fact]
    public async Task ExecuteResultAsync_WithInlineDisposition_SetsContentDispositionHeaderInline()
    {
        // Arrange
        const string fileName = "image.png";
        const string contentType = "image/png";
        var fileContent = "PNG Content"u8.ToArray();
        var stream = new MemoryStream(fileContent);
        var attachment = new FileAttachmentResult(stream, contentType, fileName, MaxFileSize);
        var result = new FileContentStreamResult(attachment, ContentDispositionType.inline);

        var httpContext = CreateMockHttpContext();
        var actionContext = new ActionContext(httpContext, new(), new(), new());

        // Act
        await result.ExecuteResultAsync(actionContext);

        // Assert
        var dispositionHeader = httpContext.Response.Headers["Content-Disposition"].ToString();
        Assert.Equal("inline", dispositionHeader);
    }

    [Fact]
    public async Task ExecuteResultAsync_SetsContentTypeHeader()
    {
        // Arrange
        const string contentType = "application/json";
        var fileContent = "{\"key\":\"value\"}"u8.ToArray();
        var stream = new MemoryStream(fileContent);
        var attachment = new FileAttachmentResult(stream, contentType, "data.json", MaxFileSize);
        var result = new FileContentStreamResult(attachment, ContentDispositionType.attachment);

        var httpContext = CreateMockHttpContext();
        var actionContext = new ActionContext(httpContext, new(), new(), new());

        // Act
        await result.ExecuteResultAsync(actionContext);

        // Assert
        Assert.Equal(contentType, httpContext.Response.ContentType);
    }

    [Fact]
    public async Task ExecuteResultAsync_ThrowsFileSizeExceededException_WhenFileSizeExceeded()
    {
        // Arrange
        const string contentType = "application/octet-stream";
        const long maxAllowedBytes = 1024; // 1 KB
        var fileContent = new byte[maxAllowedBytes + 100]; // File larger than max
        for (int i = 0; i < fileContent.Length; i++)
        {
            fileContent[i] = (byte)(i % 256);
        }

        var stream = new MemoryStream(fileContent);
        var attachment = new FileAttachmentResult(stream, contentType, "toolarge.bin", maxAllowedBytes);
        var result = new FileContentStreamResult(attachment, ContentDispositionType.attachment);

        var httpContext = CreateMockHttpContext();
        var actionContext = new ActionContext(httpContext, new(), new(), new());

        // Act & Assert
        await Assert.ThrowsAsync<FileSizeExceededException>(
            () => result.ExecuteResultAsync(actionContext));
    }

    private static HttpContext CreateMockHttpContext(CancellationToken requestAbortedToken = default)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        if (requestAbortedToken != default)
        {
            httpContext.RequestAborted = requestAbortedToken;
        }

        return httpContext;
    }
}

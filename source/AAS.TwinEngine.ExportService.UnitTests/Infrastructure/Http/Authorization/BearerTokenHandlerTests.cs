using System.Net;

using AAS.TwinEngine.ExportService.Infrastructure.Http.Authorization;

using NSubstitute;

namespace AAS.TwinEngine.ExportService.UnitTests.Infrastructure.Http.Authorization;

public class BearerTokenHandlerTests
{
    private readonly ITokenProvider _tokenProvider = Substitute.For<ITokenProvider>();

    private sealed class InnerHandler : HttpMessageHandler
    {
        public HttpRequestMessage? CapturedRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    [Fact]
    public async Task SendAsync_WhenTokenProviderReturnsToken_AttachesBearerHeader()
    {
        // Arrange
        const string endpointName = "test-endpoint";
        const string expectedToken = "secret-jwt-token";
        _tokenProvider.GetTokenAsync(endpointName, Arg.Any<CancellationToken>()).Returns(expectedToken);

        var innerHandler = new InnerHandler();
        var sut = new BearerTokenHandler(_tokenProvider, endpointName)
        {
            InnerHandler = innerHandler
        };

        var invoker = new HttpMessageInvoker(sut);
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/test");

        // Act
        var response = await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(innerHandler.CapturedRequest);
        Assert.NotNull(innerHandler.CapturedRequest.Headers.Authorization);
        Assert.Equal("Bearer", innerHandler.CapturedRequest.Headers.Authorization.Scheme);
        Assert.Equal(expectedToken, innerHandler.CapturedRequest.Headers.Authorization.Parameter);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task SendAsync_WhenTokenIsNullOrEmpty_DoesNotAttachAuthorizationHeader(string? token)
    {
        // Arrange
        const string endpointName = "test-endpoint-no-auth";
        _tokenProvider.GetTokenAsync(endpointName, Arg.Any<CancellationToken>()).Returns(token);

        var innerHandler = new InnerHandler();
        var sut = new BearerTokenHandler(_tokenProvider, endpointName)
        {
            InnerHandler = innerHandler
        };

        var invoker = new HttpMessageInvoker(sut);
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/test");

        // Act
        var response = await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(innerHandler.CapturedRequest);
        Assert.Null(innerHandler.CapturedRequest.Headers.Authorization);
    }
}

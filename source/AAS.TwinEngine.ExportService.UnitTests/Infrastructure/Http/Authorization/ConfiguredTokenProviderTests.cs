using System.Net;
using System.Text;

using AAS.TwinEngine.ExportService.ApplicationLogic.Exceptions;
using AAS.TwinEngine.ExportService.Infrastructure.Http.Authorization;
using AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

using Microsoft.Extensions.Logging;

using NSubstitute;

namespace AAS.TwinEngine.ExportService.UnitTests.Infrastructure.Http.Authorization;

public class ConfiguredTokenProviderTests
{
    private readonly IHttpClientFactory _httpClientFactory = Substitute.For<IHttpClientFactory>();
    private readonly ILogger<ConfiguredTokenProvider> _logger = Substitute.For<ILogger<ConfiguredTokenProvider>>();

    private sealed class DelegatingTestHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }

        public DelegatingTestHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
            => _handler = handler;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content is not null)
            {
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }
            return _handler(request);
        }
    }

    [Fact]
    public async Task GetTokenAsync_WhenEndpointNotConfigured_ReturnsNull()
    {
        // Arrange
        var authMap = new Dictionary<string, AuthConfig>();
        var sut = new ConfiguredTokenProvider(authMap, _httpClientFactory, _logger);

        // Act
        var token = await sut.GetTokenAsync("unknown-endpoint", CancellationToken.None);

        // Assert
        Assert.Null(token);
    }

    [Fact]
    public async Task GetTokenAsync_WhenAuthKindNone_ReturnsNull()
    {
        // Arrange
        var authMap = new Dictionary<string, AuthConfig>
        {
            ["endpoint1"] = new AuthConfig { Kind = AuthKind.None }
        };
        var sut = new ConfiguredTokenProvider(authMap, _httpClientFactory, _logger);

        // Act
        var token = await sut.GetTokenAsync("endpoint1", CancellationToken.None);

        // Assert
        Assert.Null(token);
    }

    [Fact]
    public async Task GetTokenAsync_WhenStaticBearerWithValidToken_ReturnsToken()
    {
        // Arrange
        var authMap = new Dictionary<string, AuthConfig>
        {
            ["endpoint1"] = new AuthConfig
            {
                Kind = AuthKind.StaticBearer,
                BearerToken = "my-static-token"
            }
        };
        var sut = new ConfiguredTokenProvider(authMap, _httpClientFactory, _logger);

        // Act
        var token = await sut.GetTokenAsync("endpoint1", CancellationToken.None);

        // Assert
        Assert.Equal("my-static-token", token);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetTokenAsync_WhenStaticBearerWithEmptyToken_ThrowsAuthenticationFailedException(string? bearerToken)
    {
        // Arrange
        var authMap = new Dictionary<string, AuthConfig>
        {
            ["endpoint1"] = new AuthConfig
            {
                Kind = AuthKind.StaticBearer,
                BearerToken = bearerToken
            }
        };
        var sut = new ConfiguredTokenProvider(authMap, _httpClientFactory, _logger);

        // Act & Assert
        await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            sut.GetTokenAsync("endpoint1", CancellationToken.None));
    }

    [Theory]
    [InlineData(null, "client", "secret")]
    [InlineData("", "client", "secret")]
    [InlineData("https://auth.example.com", null, "secret")]
    [InlineData("https://auth.example.com", "", "secret")]
    [InlineData("https://auth.example.com", "client", null)]
    [InlineData("https://auth.example.com", "client", "")]
    public async Task GetTokenAsync_WhenOAuthMissingRequiredFields_ThrowsAuthenticationFailedException(
        string? tokenEndpoint, string? clientId, string? clientSecret)
    {
        // Arrange
        var authMap = new Dictionary<string, AuthConfig>
        {
            ["oauth-endpoint"] = new AuthConfig
            {
                Kind = AuthKind.OAuthClientCredentials,
                TokenEndpoint = tokenEndpoint,
                ClientId = clientId,
                ClientSecret = clientSecret
            }
        };
        var sut = new ConfiguredTokenProvider(authMap, _httpClientFactory, _logger);

        // Act & Assert
        await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            sut.GetTokenAsync("oauth-endpoint", CancellationToken.None));
    }

    [Fact]
    public async Task GetTokenAsync_WhenOAuthSucceeds_ReturnsTokenAndUsesCacheOnSubsequentCalls()
    {
        // Arrange
        var callCount = 0;
        var testHandler = new DelegatingTestHandler(req =>
        {
            callCount++;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"access_token\":\"oauth-jwt-123\",\"expires_in\":3600,\"token_type\":\"Bearer\"}",
                    Encoding.UTF8,
                    "application/json")
            };
        });

        var client = new HttpClient(testHandler);
        _httpClientFactory.CreateClient(HttpClientNames.OAuthToken).Returns(client);

        var authMap = new Dictionary<string, AuthConfig>
        {
            ["oauth-endpoint"] = new AuthConfig
            {
                Kind = AuthKind.OAuthClientCredentials,
                TokenEndpoint = "https://auth.example.com/token",
                ClientId = "my-client-id",
                ClientSecret = "my-client-secret",
                Scope = "api://read"
            }
        };
        var sut = new ConfiguredTokenProvider(authMap, _httpClientFactory, _logger);

        // Act - Call 1 (fetches from server)
        var token1 = await sut.GetTokenAsync("oauth-endpoint", CancellationToken.None);

        // Act - Call 2 (uses cache)
        var token2 = await sut.GetTokenAsync("oauth-endpoint", CancellationToken.None);

        // Assert
        Assert.Equal("oauth-jwt-123", token1);
        Assert.Equal("oauth-jwt-123", token2);
        Assert.Equal(1, callCount); // Only one HTTP call made due to cache

        Assert.NotNull(testHandler.LastRequestBody);
        Assert.Contains("grant_type=client_credentials", testHandler.LastRequestBody);
        Assert.Contains("client_id=my-client-id", testHandler.LastRequestBody);
        Assert.Contains("client_secret=my-client-secret", testHandler.LastRequestBody);
        Assert.Contains("scope=api%3A%2F%2Fread", testHandler.LastRequestBody);
    }

    [Fact]
    public async Task GetTokenAsync_WhenOAuthEndpointReturnsHttpError_ThrowsAuthenticationFailedException()
    {
        // Arrange
        var testHandler = new DelegatingTestHandler(req =>
            new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("{\"error\":\"invalid_client\"}", Encoding.UTF8, "application/json")
            });

        var client = new HttpClient(testHandler);
        _httpClientFactory.CreateClient(HttpClientNames.OAuthToken).Returns(client);

        var authMap = new Dictionary<string, AuthConfig>
        {
            ["oauth-endpoint"] = new AuthConfig
            {
                Kind = AuthKind.OAuthClientCredentials,
                TokenEndpoint = "https://auth.example.com/token",
                ClientId = "client",
                ClientSecret = "secret"
            }
        };
        var sut = new ConfiguredTokenProvider(authMap, _httpClientFactory, _logger);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            sut.GetTokenAsync("oauth-endpoint", CancellationToken.None));
        Assert.Contains("401", ex.Message);
    }

    [Fact]
    public async Task GetTokenAsync_WhenOAuthResponseJsonIsEmptyOrMissingAccessToken_ThrowsAuthenticationFailedException()
    {
        // Arrange
        var testHandler = new DelegatingTestHandler(req =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });

        var client = new HttpClient(testHandler);
        _httpClientFactory.CreateClient(HttpClientNames.OAuthToken).Returns(client);

        var authMap = new Dictionary<string, AuthConfig>
        {
            ["oauth-endpoint"] = new AuthConfig
            {
                Kind = AuthKind.OAuthClientCredentials,
                TokenEndpoint = "https://auth.example.com/token",
                ClientId = "client",
                ClientSecret = "secret"
            }
        };
        var sut = new ConfiguredTokenProvider(authMap, _httpClientFactory, _logger);

        // Act & Assert
        await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            sut.GetTokenAsync("oauth-endpoint", CancellationToken.None));
    }

    [Fact]
    public async Task GetTokenAsync_WhenHttpClientThrowsException_ThrowsAuthenticationFailedExceptionWithoutExposingSecrets()
    {
        // Arrange
        var testHandler = new DelegatingTestHandler(req =>
            throw new HttpRequestException("DNS resolution failed"));

        var client = new HttpClient(testHandler);
        _httpClientFactory.CreateClient(HttpClientNames.OAuthToken).Returns(client);

        var authMap = new Dictionary<string, AuthConfig>
        {
            ["oauth-endpoint"] = new AuthConfig
            {
                Kind = AuthKind.OAuthClientCredentials,
                TokenEndpoint = "https://auth.example.com/token",
                ClientId = "client",
                ClientSecret = "super-secret-password"
            }
        };
        var sut = new ConfiguredTokenProvider(authMap, _httpClientFactory, _logger);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            sut.GetTokenAsync("oauth-endpoint", CancellationToken.None));
        Assert.DoesNotContain("super-secret-password", ex.Message);
    }

    [Fact]
    public async Task GetTokenAsync_WhenExpiresInIsSmall_CalculatesExpiryCorrectly()
    {
        // Arrange - test expiresIn <= 30 branch (e.g. 20)
        var testHandler = new DelegatingTestHandler(req =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"access_token\":\"short-lived-token\",\"expires_in\":20,\"token_type\":\"Bearer\"}",
                    Encoding.UTF8,
                    "application/json")
            });

        var client = new HttpClient(testHandler);
        _httpClientFactory.CreateClient(HttpClientNames.OAuthToken).Returns(client);

        var authMap = new Dictionary<string, AuthConfig>
        {
            ["oauth-endpoint"] = new AuthConfig
            {
                Kind = AuthKind.OAuthClientCredentials,
                TokenEndpoint = "https://auth.example.com/token",
                ClientId = "client",
                ClientSecret = "secret"
            }
        };
        var sut = new ConfiguredTokenProvider(authMap, _httpClientFactory, _logger);

        // Act
        var token = await sut.GetTokenAsync("oauth-endpoint", CancellationToken.None);

        // Assert
        Assert.Equal("short-lived-token", token);
    }
}

using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients.Caching;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

using NSubstitute;

using UnauthorizedAccessException = AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure.UnauthorizedAccessException;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Http.Clients.Caching;

public class CachedGetRequestClientTests
{
    private readonly ICreateClient _clientFactory;
    private readonly HybridCache _cache;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CachedGetRequestClient> _logger;
    private readonly CachedGetRequestClient _sut;

    public CachedGetRequestClientTests()
    {
        _clientFactory = Substitute.For<ICreateClient>();
        _cache = Substitute.For<HybridCache>();
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _logger = Substitute.For<ILogger<CachedGetRequestClient>>();

        _sut = new CachedGetRequestClient(
            _clientFactory,
            _cache,
            _httpContextAccessor,
            _logger);
    }

    [Fact]
    public async Task GetStringAsync_CacheHit_ReturnsCachedValueWithoutCallingHttp()
    {
        // Arrange
        const string RelativeUrl = "api/test";
        const string HttpClientName = "TestClient";
        const string CachedResponse = "Cached data";

        _httpContextAccessor.HttpContext.Returns((HttpContext)null!);
        SetupCacheHit(CachedResponse);

        // Act
        var result = await _sut.GetStringAsync(RelativeUrl, HttpClientName, 5, CancellationToken.None);

        // Assert
        Assert.Equal(CachedResponse, result);
        _clientFactory.DidNotReceive().CreateClient(Arg.Any<string>());
    }

    [Fact]
    public async Task GetStringAsync_CacheMiss_CallsHttpClientAndReturnsResponse()
    {
        // Arrange
        const string RelativeUrl = "api/test";
        const string HttpClientName = "TestClient";
        const string ExpectedResponse = "Direct HTTP data";

        _httpContextAccessor.HttpContext.Returns((HttpContext)null!);
        SetupCacheMiss();

        using var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ExpectedResponse)
        };
        SetupHttpClient(HttpClientName, httpResponse);

        // Act
        var result = await _sut.GetStringAsync(RelativeUrl, HttpClientName, 5, CancellationToken.None);

        // Assert
        Assert.Equal(ExpectedResponse, result);
        _clientFactory.Received(1).CreateClient(HttpClientName);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, typeof(ResourceNotFoundException))]
    [InlineData(HttpStatusCode.Unauthorized, typeof(UnauthorizedAccessException))]
    [InlineData(HttpStatusCode.Forbidden, typeof(UnauthorizedAccessException))]
    [InlineData(HttpStatusCode.RequestTimeout, typeof(RequestTimeoutException))]
    [InlineData(HttpStatusCode.InternalServerError, typeof(ValidationFailedException))]
    [InlineData(HttpStatusCode.BadRequest, typeof(ValidationFailedException))]
    public async Task GetStringAsync_CacheMiss_MapsHttpStatusCodesToExpectedExceptions(HttpStatusCode statusCode, Type expectedExceptionType)
    {
        // Arrange
        const string RelativeUrl = "api/test";
        const string HttpClientName = "TestClient";

        _httpContextAccessor.HttpContext.Returns((HttpContext)null!);
        SetupCacheMiss();

        using var httpResponse = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent("Error response body")
        };
        SetupHttpClient(HttpClientName, httpResponse);

        // Act & Assert
        await Assert.ThrowsAsync(expectedExceptionType, () =>
            _sut.GetStringAsync(RelativeUrl, HttpClientName, 5, CancellationToken.None));
    }

    [Fact]
    public async Task GetStringAsync_BuildsExpectedCacheKey_ForNullHttpContext()
    {
        // Arrange
        const string RelativeUrl = "api/test";
        _httpContextAccessor.HttpContext.Returns((HttpContext)null!);

        string? capturedKey = null;
        SetupCacheCapture(key => capturedKey = key);

        // Act
        await _sut.GetStringAsync(RelativeUrl, "client", 5, CancellationToken.None);

        // Assert
        var expectedHash = ComputeHash(RelativeUrl);
        Assert.Equal($"anonymous:req:{expectedHash}", capturedKey);
    }

    [Fact]
    public async Task GetStringAsync_BuildsExpectedCacheKey_ForNullUser()
    {
        // Arrange
        const string RelativeUrl = "api/test";
        var httpContext = new DefaultHttpContext { User = null! };
        _httpContextAccessor.HttpContext.Returns(httpContext);

        string? capturedKey = null;
        SetupCacheCapture(key => capturedKey = key);

        // Act
        await _sut.GetStringAsync(RelativeUrl, "client", 5, CancellationToken.None);

        // Assert
        var expectedHash = ComputeHash(RelativeUrl);
        Assert.Equal($"anonymous:req:{expectedHash}", capturedKey);
    }

    [Fact]
    public async Task GetStringAsync_BuildsExpectedCacheKey_ForUnauthenticatedUser()
    {
        // Arrange
        const string RelativeUrl = "api/test";
        var httpContext = new DefaultHttpContext();
        var identity = new ClaimsIdentity(); // IsAuthenticated = false
        httpContext.User = new ClaimsPrincipal(identity);
        _httpContextAccessor.HttpContext.Returns(httpContext);

        string? capturedKey = null;
        SetupCacheCapture(key => capturedKey = key);

        // Act
        await _sut.GetStringAsync(RelativeUrl, "client", 5, CancellationToken.None);

        // Assert
        var expectedHash = ComputeHash(RelativeUrl);
        Assert.Equal($"anonymous:req:{expectedHash}", capturedKey);
    }

    [Fact]
    public async Task GetStringAsync_BuildsExpectedCacheKey_ForAuthenticatedUserWithNameIdentifier()
    {
        // Arrange
        const string RelativeUrl = "api/test";
        const string UserId = "user123";
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, UserId),
            new("role", "admin")
        };

        var httpContext = new DefaultHttpContext();
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        httpContext.User = new ClaimsPrincipal(identity);
        _httpContextAccessor.HttpContext.Returns(httpContext);

        string? capturedKey = null;
        SetupCacheCapture(key => capturedKey = key);

        // Act
        await _sut.GetStringAsync(RelativeUrl, "client", 5, CancellationToken.None);

        // Assert
        var requestHash = ComputeHash(RelativeUrl);
        var permissionHash = ComputePermissionHash(claims);
        Assert.Equal($"user:{UserId}:ph:{permissionHash}:req:{requestHash}", capturedKey);
    }

    [Fact]
    public async Task GetStringAsync_BuildsExpectedCacheKey_ForAuthenticatedUserWithSub()
    {
        // Arrange
        const string RelativeUrl = "api/test";
        const string SubId = "sub456";
        var claims = new List<Claim>
        {
            new("sub", SubId),
            new("role", "user")
        };

        var httpContext = new DefaultHttpContext();
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        httpContext.User = new ClaimsPrincipal(identity);
        _httpContextAccessor.HttpContext.Returns(httpContext);

        string? capturedKey = null;
        SetupCacheCapture(key => capturedKey = key);

        // Act
        await _sut.GetStringAsync(RelativeUrl, "client", 5, CancellationToken.None);

        // Assert
        var requestHash = ComputeHash(RelativeUrl);
        var permissionHash = ComputePermissionHash(claims);
        Assert.Equal($"user:{SubId}:ph:{permissionHash}:req:{requestHash}", capturedKey);
    }

    [Fact]
    public async Task GetStringAsync_BuildsExpectedCacheKey_ForAuthenticatedUserFallsBackToUnknown()
    {
        // Arrange
        const string RelativeUrl = "api/test";
        var claims = new List<Claim>
        {
            new("role", "user")
        };

        var httpContext = new DefaultHttpContext();
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        httpContext.User = new ClaimsPrincipal(identity);
        _httpContextAccessor.HttpContext.Returns(httpContext);

        string? capturedKey = null;
        SetupCacheCapture(key => capturedKey = key);

        // Act
        await _sut.GetStringAsync(RelativeUrl, "client", 5, CancellationToken.None);

        // Assert
        var requestHash = ComputeHash(RelativeUrl);
        var permissionHash = ComputePermissionHash(claims);
        Assert.Equal($"user:unknown:ph:{permissionHash}:req:{requestHash}", capturedKey);
    }

    [Fact]
    public async Task GetStringAsync_BuildsExpectedCacheKey_ClaimsOrderingDoesNotAffectPermissionHash()
    {
        // Arrange
        const string RelativeUrl = "api/test";
        var claimsA = new List<Claim>
        {
            new("role", "admin"),
            new("permission", "read"),
            new(ClaimTypes.NameIdentifier, "user123")
        };
        var claimsB = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "user123"),
            new("permission", "read"),
            new("role", "admin")
        };

        var httpContextA = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claimsA, "Test")) };
        var httpContextB = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claimsB, "Test")) };

        string? capturedKeyA = null;
        string? capturedKeyB = null;

        SetupCacheCapture(key => capturedKeyA = key);
        _httpContextAccessor.HttpContext.Returns(httpContextA);
        await _sut.GetStringAsync(RelativeUrl, "client", 5, CancellationToken.None);

        SetupCacheCapture(key => capturedKeyB = key);
        _httpContextAccessor.HttpContext.Returns(httpContextB);
        await _sut.GetStringAsync(RelativeUrl, "client", 5, CancellationToken.None);

        // Assert
        Assert.Equal(capturedKeyA, capturedKeyB);
    }

    [Fact]
    public async Task GetStringAsync_ConfiguresHybridCacheEntryOptionsCorrectly()
    {
        // Arrange
        const string RelativeUrl = "api/test";
        const int ExpirationTimeMinutes = 15;
        _httpContextAccessor.HttpContext.Returns((HttpContext)null!);

        HybridCacheEntryOptions? capturedOptions = null;
        SetupCacheCapture(key => { }, options => capturedOptions = options);

        // Act
        await _sut.GetStringAsync(RelativeUrl, "client", ExpirationTimeMinutes, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedOptions);
        Assert.Equal(TimeSpan.FromMinutes(ExpirationTimeMinutes), capturedOptions.Expiration);
        Assert.Equal(TimeSpan.FromMinutes(ExpirationTimeMinutes), capturedOptions.LocalCacheExpiration);
    }

    [Fact]
    public async Task GetStringAsync_PropagatesCancellationToken()
    {
        // Arrange
        const string RelativeUrl = "api/test";
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        _httpContextAccessor.HttpContext.Returns((HttpContext)null!);

        var capturedToken = CancellationToken.None;
        SetupCacheCapture(key => { }, null, token => capturedToken = token);

        // Act
        await _sut.GetStringAsync(RelativeUrl, "client", 5, cancellationToken);

        // Assert
        Assert.Equal(cancellationToken, capturedToken);
    }

    [Theory]
    [InlineData("?isCacheEnable=false", true)]
    [InlineData("?isCacheEnable=true", false)]
    [InlineData("?isCacheEnable=invalid", false)]
    [InlineData("", false)]
    public async Task GetStringAsync_RespectsIsCacheEnabledQueryParameter(string queryString, bool expectBypass)
    {
        // Arrange
        const string RelativeUrl = "api/test";
        const string HttpClientName = "TestClient";
        const string ExpectedResponse = "Direct HTTP data";

        var httpContext = new DefaultHttpContext();
        if (!string.IsNullOrEmpty(queryString))
        {
            httpContext.Request.QueryString = new QueryString(queryString);
        }
        _httpContextAccessor.HttpContext.Returns(httpContext);

        if (expectBypass)
        {
            // If bypassed, cache is never called.
            _cache.GetOrCreateAsync<string>(
                Arg.Any<string>(),
                Arg.Any<Func<CancellationToken, ValueTask<string>>>(),
                Arg.Any<HybridCacheEntryOptions>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>()
            ).Returns(new ValueTask<string>("Cached data that should be bypassed"));
        }
        else
        {
            // If cache is enabled, SetupCacheHit will return direct response.
            SetupCacheHit(ExpectedResponse);
        }

        using var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ExpectedResponse)
        };
        SetupHttpClient(HttpClientName, httpResponse);

        // Act
        var result = await _sut.GetStringAsync(RelativeUrl, HttpClientName, 5, CancellationToken.None);

        // Assert
        Assert.Equal(ExpectedResponse, result);

        if (expectBypass)
        {
            // If bypassed, HTTP client must be called directly.
            _clientFactory.Received(1).CreateClient(HttpClientName);
            // And cache should not be queried.
            _cache.DidNotReceive().GetOrCreateAsync<string>(
                Arg.Any<string>(),
                Arg.Any<Func<CancellationToken, ValueTask<string>>>(),
                Arg.Any<HybridCacheEntryOptions>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>()
            );
        }
        else
        {
            // If cache was not bypassed, the HTTP client should not be called since we mocked a cache hit.
            _clientFactory.DidNotReceive().CreateClient(Arg.Any<string>());
        }
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }

    private static string ComputePermissionHash(IEnumerable<Claim> claims)
    {
        var claimsString = string.Join("|", claims
            .OrderBy(c => c.Type, StringComparer.Ordinal)
            .ThenBy(c => c.Value, StringComparer.Ordinal)
            .Select(c => $"{c.Type}={c.Value}"));

        return ComputeHash(claimsString);
    }

    private void SetupCacheHit(string response)
    {
        _cache.GetOrCreateAsync<string>(
            Arg.Any<string>(),
            Arg.Any<Func<CancellationToken, ValueTask<string>>>(),
            Arg.Any<HybridCacheEntryOptions>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<CancellationToken>()
        ).Returns(new ValueTask<string>(response));
    }

    private void SetupCacheMiss()
    {
        _cache.GetOrCreateAsync<string>(
            Arg.Any<string>(),
            Arg.Any<Func<CancellationToken, ValueTask<string>>>(),
            Arg.Any<HybridCacheEntryOptions>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<CancellationToken>()
        ).Returns(info =>
        {
            var factory = info.ArgAt<Func<CancellationToken, ValueTask<string>>>(1);
            return factory(CancellationToken.None);
        });
    }

    private void SetupCacheCapture(Action<string> onKeyCaptured, Action<HybridCacheEntryOptions>? onOptionsCaptured = null, Action<CancellationToken>? onTokenCaptured = null)
    {
        _cache.GetOrCreateAsync(
            Arg.Do<string>(key => onKeyCaptured(key)),
            Arg.Any<Func<CancellationToken, ValueTask<string>>>(),
            Arg.Do<HybridCacheEntryOptions>(options => onOptionsCaptured?.Invoke(options)),
            Arg.Any<IEnumerable<string>>(),
            Arg.Do<CancellationToken>(token => onTokenCaptured?.Invoke(token))
        ).Returns(new ValueTask<string>(""));
    }

    private void SetupHttpClient(string httpClientName, HttpResponseMessage response)
    {
        var messageHandler = new FakeHttpMessageHandler((req, token) => Task.FromResult(response));
        var httpClient = new HttpClient(messageHandler)
        {
            BaseAddress = new Uri("http://localhost/")
        };

        _clientFactory.CreateClient(httpClientName).Returns(httpClient);
    }
}

internal sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler = handler;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => _handler(request, cancellationToken);
}

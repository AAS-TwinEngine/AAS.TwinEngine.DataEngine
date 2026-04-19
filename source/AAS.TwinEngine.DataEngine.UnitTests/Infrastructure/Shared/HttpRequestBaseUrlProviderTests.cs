using AAS.TwinEngine.DataEngine.Infrastructure.Shared;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Shared;

public class HttpRequestBaseUrlProviderTests
{
    private readonly IHttpContextAccessor _httpContextAccessor = Substitute.For<IHttpContextAccessor>();

    private static IOptions<GeneralConfig> CreateOptions(Uri? baseUrl, string allowedHosts = "*")
    {
        return Options.Create(new GeneralConfig
        {
            DataEngineRepositoryBaseUrl = baseUrl,
            AllowedHosts = allowedHosts
        });
    }

    private static DefaultHttpContext CreateHttpContext(string scheme = "https", string host = "example.com")
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = scheme;
        context.Request.Host = new HostString(host);
        return context;
    }

    [Fact]
    public void GetBaseUrl_WithConfiguredUrl_ReturnsConfiguredValue()
    {
        // Arrange
        var configuredUrl = new Uri("https://configured.com/");
        var options = CreateOptions(configuredUrl);

        var sut = new HttpRequestBaseUrlProvider(_httpContextAccessor, options);

        // Act
        var result = sut.GetBaseUrl();

        // Assert
        Assert.Equal(configuredUrl, result);
    }

    [Fact]
    public void GetBaseUrl_WithDynamicUrl_ExtractsFromHttpRequest()
    {
        // Arrange
        var context = CreateHttpContext("https", "mydomain.com");
        _httpContextAccessor.HttpContext.Returns(context);

        var options = CreateOptions(null);
        var sut = new HttpRequestBaseUrlProvider(_httpContextAccessor, options);

        // Act
        var result = sut.GetBaseUrl();

        // Assert
        Assert.Equal(new Uri("https://mydomain.com"), result);
    }

    [Fact]
    public void GetBaseUrl_WithNullHttpContext_ThrowsInvalidOperationException()
    {
        // Arrange
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        var options = CreateOptions(null);
        var sut = new HttpRequestBaseUrlProvider(_httpContextAccessor, options);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => sut.GetBaseUrl());
        Assert.Contains("No HTTP request context", ex.Message);
    }

    [Fact]
    public void GetBaseUrl_WithMaliciousHostHeader_ThrowsWhenAllowedHostsConfigured()
    {
        // Arrange
        var context = CreateHttpContext("https", "evil.com");
        _httpContextAccessor.HttpContext.Returns(context);

        var options = CreateOptions(null, allowedHosts: "example.com;mydomain.com");
        var sut = new HttpRequestBaseUrlProvider(_httpContextAccessor, options);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => sut.GetBaseUrl());
        Assert.Contains("not in the configured AllowedHosts", ex.Message);
    }

    [Fact]
    public void GetBaseUrl_WithAllowedHost_ReturnsUrl()
    {
        // Arrange
        var context = CreateHttpContext("https", "mydomain.com");
        _httpContextAccessor.HttpContext.Returns(context);

        var options = CreateOptions(null, allowedHosts: "example.com;mydomain.com");
        var sut = new HttpRequestBaseUrlProvider(_httpContextAccessor, options);

        // Act
        var result = sut.GetBaseUrl();

        // Assert
        Assert.Equal(new Uri("https://mydomain.com"), result);
    }

    [Fact]
    public void GetBaseUrl_WithWildcardAllowedHosts_PermitsAnyHost()
    {
        // Arrange
        var context = CreateHttpContext("https", "anyhost.com");
        _httpContextAccessor.HttpContext.Returns(context);

        var options = CreateOptions(null, allowedHosts: "*");
        var sut = new HttpRequestBaseUrlProvider(_httpContextAccessor, options);

        // Act
        var result = sut.GetBaseUrl();

        // Assert
        Assert.Equal(new Uri("https://anyhost.com"), result);
    }

    [Fact]
    public void GetBaseUrl_WithDifferentCaseHost_PermitsHost()
    {
        // Arrange
        var context = CreateHttpContext("https", "MyDomain.COM");
        _httpContextAccessor.HttpContext.Returns(context);

        var options = CreateOptions(null, allowedHosts: "mydomain.com");
        var sut = new HttpRequestBaseUrlProvider(_httpContextAccessor, options);

        // Act
        var result = sut.GetBaseUrl();

        // Assert
        Assert.Equal(new Uri("https://MyDomain.COM"), result);
    }
}

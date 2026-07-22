using System.Security.Claims;

using AAS.TwinEngine.DataEngine.Infrastructure.Caching;

using Microsoft.AspNetCore.Http;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Caching;

public class CacheKeyBuilderTests
{
    [Fact]
    public void BuildCacheKey_ReturnsAuthenticatedFormat_WhenUserIsAuthenticated()
    {
        var httpContextAccessor = CreateAuthenticatedAccessor("user-123",
        [
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("scope", "read")
        ]);

        var cacheKey = CacheKeyBuilder.BuildCacheKey(httpContextAccessor, "Method", "param1");

        Assert.StartsWith("user:user-123:ph:", cacheKey);
        Assert.Contains(":req:", cacheKey);
    }

    [Fact]
    public void BuildCacheKey_ReturnsAnonymousFormat_WhenNoUserToken()
    {
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        var cacheKey = CacheKeyBuilder.BuildCacheKey(httpContextAccessor, "Method", "param1");

        Assert.StartsWith("anonymous:req:", cacheKey);
    }

    [Fact]
    public void BuildCacheKey_ReturnsAnonymousFormat_WhenUserIsNotAuthenticated()
    {
        var httpContext = new DefaultHttpContext();

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        var cacheKey = CacheKeyBuilder.BuildCacheKey(httpContextAccessor, "Method", "param1");

        Assert.StartsWith("anonymous:req:", cacheKey);
    }

    [Fact]
    public void BuildCacheKey_UsesSub_WhenNameIdentifierClaimMissing()
    {
        var claims = new[] { new Claim("sub", "sub-user-456") };
        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        var cacheKey = CacheKeyBuilder.BuildCacheKey(httpContextAccessor, "Method", "param1");

        Assert.StartsWith("user:sub-user-456:ph:", cacheKey);
    }

    [Fact]
    public void BuildCacheKey_PermissionHashChanges_WhenClaimsChange()
    {
        var accessor1 = CreateAuthenticatedAccessor("user-1",
        [
            new Claim(ClaimTypes.Role, "Admin")
        ]);

        var accessor2 = CreateAuthenticatedAccessor("user-1",
        [
            new Claim(ClaimTypes.Role, "Reader")
        ]);

        var key1 = CacheKeyBuilder.BuildCacheKey(accessor1, "Method", "param1");
        var key2 = CacheKeyBuilder.BuildCacheKey(accessor2, "Method", "param1");

        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void BuildCacheKey_RequestHashChanges_WhenParametersChange()
    {
        var accessor = CreateAuthenticatedAccessor("user-1",
        [
            new Claim(ClaimTypes.Role, "Admin")
        ]);

        var key1 = CacheKeyBuilder.BuildCacheKey(accessor, "Method", "templateA");
        var key2 = CacheKeyBuilder.BuildCacheKey(accessor, "Method", "templateB");

        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void BuildCacheKey_ReturnsSameKey_ForSameInputs()
    {
        var accessor1 = CreateAuthenticatedAccessor("user-1",
        [
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("scope", "read")
        ]);

        var accessor2 = CreateAuthenticatedAccessor("user-1",
        [
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("scope", "read")
        ]);

        var key1 = CacheKeyBuilder.BuildCacheKey(accessor1, "Method", "param1");
        var key2 = CacheKeyBuilder.BuildCacheKey(accessor2, "Method", "param1");

        Assert.Equal(key1, key2);
    }

    [Fact]
    public void ComputePermissionHash_IsOrderIndependent()
    {
        var claims1 = new[]
        {
            new Claim("scope", "read"),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var claims2 = new[]
        {
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("scope", "read")
        };

        var hash1 = CacheKeyBuilder.ComputePermissionHash(claims1);
        var hash2 = CacheKeyBuilder.ComputePermissionHash(claims2);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void BuildCacheKey_AnonymousReturnsSameKey_ForSameParameters()
    {
        var accessor1 = Substitute.For<IHttpContextAccessor>();
        accessor1.HttpContext.Returns((HttpContext?)null);

        var accessor2 = Substitute.For<IHttpContextAccessor>();
        accessor2.HttpContext.Returns((HttpContext?)null);

        var key1 = CacheKeyBuilder.BuildCacheKey(accessor1, "Method", "param1");
        var key2 = CacheKeyBuilder.BuildCacheKey(accessor2, "Method", "param1");

        Assert.Equal(key1, key2);
    }

    private static IHttpContextAccessor CreateAuthenticatedAccessor(string userId, Claim[] additionalClaims)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        claims.AddRange(additionalClaims);

        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        return httpContextAccessor;
    }
}

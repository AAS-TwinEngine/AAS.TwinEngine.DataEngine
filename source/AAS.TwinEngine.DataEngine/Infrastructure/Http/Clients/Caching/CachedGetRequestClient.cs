using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.Infrastructure.Logging;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using UnauthorizedAccessException = AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure.UnauthorizedAccessException;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients.Caching;

public sealed class CachedGetRequestClient(
    ICreateClient clientFactory,
    HybridCache cache,
    IHttpContextAccessor httpContextAccessor,
    ILogger<CachedGetRequestClient> logger) : ICachedGetRequestClient
{
    public async Task<string> GetStringAsync(string relativeUrl, string httpClientName, int expirationTime, CancellationToken cancellationToken)
    {
        if (!IsCacheEnabled(httpContextAccessor))
        {
            logger.LogInformation("Cache bypassed because 'isCacheEnable=false' was specified.");
            return await FetchAsync(relativeUrl, httpClientName, cancellationToken).ConfigureAwait(false);
        }

        var cacheKey = BuildCacheKey(httpContextAccessor, relativeUrl);

        var entryOptions = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMinutes(expirationTime),
            LocalCacheExpiration = TimeSpan.FromMinutes(expirationTime)
        };

        return await cache.GetOrCreateAsync(
            cacheKey,
            async token => await FetchAsync(relativeUrl, httpClientName, token).ConfigureAwait(false),
            options: entryOptions,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> FetchAsync(string url, string httpClientName, CancellationToken cancellationToken)
    {
        logger.LogInformation("Sending HTTP GET request to {Url}", LogSanitizerExtension.Sanitize(url));

        var httpClient = clientFactory.CreateClient(httpClientName);
        var relativeUri = new Uri(url, UriKind.Relative);

        var response = await httpClient.GetAsync(relativeUri, cancellationToken).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            logger.LogInformation("Received successful HTTP response with status code: {StatusCode}", response.StatusCode);
            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        logger.LogError("HTTP GET failed with status {StatusCode}. Response: {ResponseMessage}", response.StatusCode, responseContent);

        throw response.StatusCode switch
        {
            HttpStatusCode.NotFound => new ResourceNotFoundException(),
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => new UnauthorizedAccessException(),
            HttpStatusCode.RequestTimeout => new RequestTimeoutException(),
            _ => new ValidationFailedException()
        };
    }

    private static string BuildCacheKey(IHttpContextAccessor httpContextAccessor, string requestParts)
    {
        var requestHash = ComputeHash(requestParts);

        var user = httpContextAccessor.HttpContext?.User;

        if (user?.Identity is { IsAuthenticated: true })
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? user.FindFirst("sub")?.Value
                         ?? "unknown";

            var permissionHash = ComputePermissionHash(user.Claims);

            return $"user:{userId}:ph:{permissionHash}:req:{requestHash}";
        }

        return $"anonymous:req:{requestHash}";
    }

    private static string ComputePermissionHash(IEnumerable<Claim> claims)
    {
        var claimsString = string.Join("|", claims
            .OrderBy(c => c.Type, StringComparer.Ordinal)
            .ThenBy(c => c.Value, StringComparer.Ordinal)
            .Select(c => $"{c.Type}={c.Value}"));

        return ComputeHash(claimsString);
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }

    private static bool IsCacheEnabled(IHttpContextAccessor httpContextAccessor)
    {
        var query = httpContextAccessor.HttpContext?.Request.Query;

        if (query is null)
        {
            return true;
        }

        if (!query.TryGetValue("isCacheEnable", out var value))
        {
            return true;
        }

        return !bool.TryParse(value, out var enabled) || enabled;
    }
}

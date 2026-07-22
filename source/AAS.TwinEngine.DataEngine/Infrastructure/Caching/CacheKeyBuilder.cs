using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using Microsoft.AspNetCore.Http;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Caching;

public static class CacheKeyBuilder
{
    public static string BuildCacheKey(IHttpContextAccessor httpContextAccessor, params string[] requestParts)
    {
        var requestHash = ComputeHash(string.Join(":", requestParts));

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

    public static string ComputePermissionHash(IEnumerable<Claim> claims)
    {
        var claimsString = string.Join("|", claims
            .OrderBy(c => c.Type, StringComparer.Ordinal)
            .ThenBy(c => c.Value, StringComparer.Ordinal)
            .Select(c => $"{c.Type}={c.Value}"));

        return ComputeHash(claimsString);
    }

    public static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }
}

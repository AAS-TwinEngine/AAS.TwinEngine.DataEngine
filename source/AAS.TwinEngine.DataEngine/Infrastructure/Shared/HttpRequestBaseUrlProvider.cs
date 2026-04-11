using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Shared;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Shared;

/// <summary>
/// Resolves the DataEngine's own base URL.
/// <list type="bullet">
///   <item><b>V1 (old config):</b> If <see cref="GeneralConfig.DataEngineRepositoryBaseUrl"/> was populated
///         by the legacy adapter, that value is used.</item>
///   <item><b>V2 (new config):</b> The property is <c>null</c>, so the URL is derived from the incoming
///         HTTP request (<c>Scheme://Host</c>).</item>
/// </list>
/// The Host header is validated against <see cref="GeneralConfig.AllowedHosts"/> to prevent
/// Host Header Injection attacks (OWASP A05:2021).
/// </summary>
public class HttpRequestBaseUrlProvider(
    IHttpContextAccessor httpContextAccessor,
    IOptions<GeneralConfig> generalConfig) : IBaseUrlProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly Uri? _configuredBaseUrl = generalConfig.Value.DataEngineRepositoryBaseUrl;
    private readonly string _allowedHosts = generalConfig.Value.AllowedHosts;

    public Uri GetBaseUrl()
    {
        if (_configuredBaseUrl != null)
        {
            return _configuredBaseUrl;
        }

        var request = _httpContextAccessor.HttpContext?.Request
            ?? throw new InvalidOperationException("No HTTP request context available — cannot derive base URL.");

        if (!IsHostAllowed(request.Host.Host))
        {
            throw new InvalidOperationException(
                $"Host header '{request.Host}' is not in the configured AllowedHosts.");
        }

        var baseUrl = $"{request.Scheme}://{request.Host}";
        return new Uri(baseUrl, UriKind.Absolute);
    }

    private bool IsHostAllowed(string host)
    {
        if (string.IsNullOrEmpty(_allowedHosts) || _allowedHosts == "*")
        {
            return true;
        }

        var allowed = _allowedHosts.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return allowed.Any(a => string.Equals(a, host, StringComparison.OrdinalIgnoreCase));
    }
}

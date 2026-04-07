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
/// </summary>
public class HttpRequestBaseUrlProvider(
    IHttpContextAccessor httpContextAccessor,
    IOptions<GeneralConfig> generalConfig) : IBaseUrlProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly Uri? _configuredBaseUrl = generalConfig.Value.DataEngineRepositoryBaseUrl;

    public Uri GetBaseUrl()
    {
        if (_configuredBaseUrl != null)
        {
            return _configuredBaseUrl;
        }

        var request = _httpContextAccessor.HttpContext?.Request
            ?? throw new InvalidOperationException("No HTTP request context available — cannot derive base URL.");

        var baseUrl = $"{request.Scheme}://{request.Host}";
        return new Uri(baseUrl.TrimEnd('/') + "/");
    }
}

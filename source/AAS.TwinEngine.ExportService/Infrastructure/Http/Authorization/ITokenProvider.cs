namespace AAS.TwinEngine.ExportService.Infrastructure.Http.Authorization;

/// <summary>
/// Returns a bearer token for a specific named endpoint. Implementations decide how the
/// token is obtained (static, OAuth client credentials, etc.) and are responsible for
/// caching where relevant.
/// </summary>
public interface ITokenProvider
{
    /// <summary>
    /// Returns a bearer token, or <c>null</c> if the endpoint requires no authentication.
    /// </summary>
    Task<string?> GetTokenAsync(string endpointName, CancellationToken cancellationToken);
}

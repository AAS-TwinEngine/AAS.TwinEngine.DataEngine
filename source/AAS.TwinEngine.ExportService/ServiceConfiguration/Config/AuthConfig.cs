namespace AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

public enum AuthKind
{
    None = 0,
    StaticBearer = 1,
    OAuthClientCredentials = 2
}

/// <summary>
/// Authentication settings for one outgoing endpoint.
/// Credentials must be supplied via environment variables or secrets stores.
/// </summary>
public sealed class AuthConfig
{
    public AuthKind Kind { get; set; } = AuthKind.None;

    /// <summary>
    /// Pre-issued bearer token. Used when <see cref="Kind"/> is <see cref="AuthKind.StaticBearer"/>.
    /// </summary>
    public string? BearerToken { get; set; }

    /// <summary>
    /// OAuth 2.0 token endpoint. Used when <see cref="Kind"/> is <see cref="AuthKind.OAuthClientCredentials"/>.
    /// </summary>
    public string? TokenEndpoint { get; set; }

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    public string? Scope { get; set; }
}

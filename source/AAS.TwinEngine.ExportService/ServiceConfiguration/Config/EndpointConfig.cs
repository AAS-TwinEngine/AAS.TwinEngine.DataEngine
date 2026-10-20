using System.Diagnostics.CodeAnalysis;

namespace AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

/// <summary>
/// A single outgoing HTTP endpoint (URL + auth). Used both for reading from source systems
/// and for writing to target systems.
/// </summary>
public sealed class EndpointConfig
{
    /// <summary>
    /// Base URL of the target system (e.g. <c>http://data-engine:8080</c>).
    /// </summary>
    [SuppressMessage("Design", "CA1056:Uri properties should not be strings", Justification = "String configuration preserves the JSON/environment binding contract; validation parses it before use.")]
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Relative endpoint path (e.g. <c>/api/v3.0/shells</c>).
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Relative health-check path for the endpoint.
    /// </summary>
    public string HealthEndpoint { get; set; } = "/healthz";

    /// <summary>
    /// Maximum number of entities requested per source page.
    /// </summary>
    public int Limit { get; set; } = 250;

    /// <summary>
    /// Source-only switch controlling export of this entity type. Ignored on target endpoints.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Authentication configuration for this endpoint. When null, the endpoint is called
    /// without authentication.
    /// </summary>
    public AuthConfig? Auth { get; set; }
}

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
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Relative endpoint path (e.g. <c>/api/v3.0/shells</c>).
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// When <c>false</c>, this entity type is skipped by the exporter.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Authentication configuration for this endpoint. When null, the endpoint is called
    /// without authentication.
    /// </summary>
    public AuthConfig? Auth { get; set; }
}

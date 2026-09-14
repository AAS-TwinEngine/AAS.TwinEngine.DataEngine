namespace AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

/// <summary>
/// OpenTelemetry exporter configuration. Mirrors DataEngine's settings for consistency.
/// </summary>
public sealed class ObservabilityConfig
{
    public string OtlpEndpoint { get; set; } = "http://localhost:4317";

    public string ServiceName { get; set; } = "ExportService";

    public string ServiceVersion { get; set; } = "1.0.0";
}

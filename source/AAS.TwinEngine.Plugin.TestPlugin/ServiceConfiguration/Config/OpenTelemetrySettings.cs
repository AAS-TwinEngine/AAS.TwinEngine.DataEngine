namespace Aas.TwinEngine.Plugin.TestPlugin.ServiceConfiguration.Config;

public class OpenTelemetrySettings
{
    public const string Section = "OpenTelemetry";
    public string OtlpEndpoint { get; set; } = "http://localhost:4317";
    public string ServiceName { get; set; } = "TestPlugin";
    public string ServiceVersion { get; set; } = "1.0.0";
}

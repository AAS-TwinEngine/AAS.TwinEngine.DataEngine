namespace AAS.TwinEngine.DataEngine.Infrastructure.Monitoring;

public class PluginManifestHealthStatus : IPluginManifestHealthStatus
{
    public bool IsHealthy { get; set; } = true;
}

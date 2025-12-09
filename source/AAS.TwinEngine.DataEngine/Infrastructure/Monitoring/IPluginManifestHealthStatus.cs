namespace AAS.TwinEngine.DataEngine.Infrastructure.Monitoring;

public interface IPluginManifestHealthStatus
{
    bool IsHealthy { get; set; }
}

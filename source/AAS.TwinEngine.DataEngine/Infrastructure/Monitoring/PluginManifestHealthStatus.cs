namespace AAS.TwinEngine.DataEngine.Infrastructure.Monitoring;

public sealed class PluginManifestHealthStatus : IPluginManifestHealthStatus
{
    private volatile bool _isHealthy = true;

    public bool IsHealthy
    {
        get => _isHealthy;
        set => _isHealthy = value;
    }
}

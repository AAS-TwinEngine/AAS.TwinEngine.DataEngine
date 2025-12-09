using AAS.TwinEngine.DataEngine.Infrastructure.Monitoring;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Monitoring;

public class PluginManifestHealthStatusTests
{
    [Fact]
    public void Default_IsHealthy_IsTrue()
    {
        var svc = new PluginManifestHealthStatus();
        Assert.True(svc.IsHealthy);
    }

    [Fact]
    public void Can_Set_IsHealthy_ToFalse()
    {
        var svc = new PluginManifestHealthStatus();
        svc.IsHealthy = false;
        Assert.False(svc.IsHealthy);
    }

    [Fact]
    public void Can_Set_IsHealthy_BackToTrue()
    {
        var svc = new PluginManifestHealthStatus { IsHealthy = false };
        svc.IsHealthy = true;
        Assert.True(svc.IsHealthy);
    }
}

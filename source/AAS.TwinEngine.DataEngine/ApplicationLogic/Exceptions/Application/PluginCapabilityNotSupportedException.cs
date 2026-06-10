using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

public class PluginCapabilityNotSupportedException : FeatureNotSupportedException
{
    public const string ServiceName = "Plugin Capability Not Supported.";

    public PluginCapabilityNotSupportedException() : base(ServiceName) { }

    public PluginCapabilityNotSupportedException(Exception ex) : base(ServiceName, ex) { }
}

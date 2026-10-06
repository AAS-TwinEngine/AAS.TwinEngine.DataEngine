using NotImplementedException = AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base.NotImplementedException;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

public class PluginCapabilityNotSupportedException : NotImplementedException
{
    public const string ServiceName = "Requested Operation Not Supported.";

    public PluginCapabilityNotSupportedException() : base(ServiceName) { }

    public PluginCapabilityNotSupportedException(Exception ex) : base(ServiceName, ex) { }
}

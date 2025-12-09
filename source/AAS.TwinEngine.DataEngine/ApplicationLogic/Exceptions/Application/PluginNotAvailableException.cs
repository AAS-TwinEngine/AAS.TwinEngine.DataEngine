using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

public class PluginNotAvailableException : ServiceUnavailableException
{
    public const string ServiceName = "Plugin";

    public PluginNotAvailableException() : base(ServiceName) { }

    public PluginNotAvailableException(Exception ex) : base(ServiceName, ex) { }
}

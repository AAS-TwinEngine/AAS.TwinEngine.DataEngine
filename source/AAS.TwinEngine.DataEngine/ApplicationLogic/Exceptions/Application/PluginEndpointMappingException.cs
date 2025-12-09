using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

public class PluginEndpointMappingException : InternalServerException
{
    public const string DefaultMessage = "Failed to resolve plugin endpoint.";

    public PluginEndpointMappingException() : base(DefaultMessage) { }

    public PluginEndpointMappingException(Exception ex) : base(DefaultMessage, ex) { }
}

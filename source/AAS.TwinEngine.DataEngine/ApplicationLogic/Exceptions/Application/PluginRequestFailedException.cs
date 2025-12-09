using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

public class PluginRequestFailedException : InternalServerException
{
    public const string DefaultMessage = "Request could not be processed.";

    public PluginRequestFailedException() : base(DefaultMessage) { }

    public PluginRequestFailedException(Exception ex) : base(DefaultMessage, ex) { }
}

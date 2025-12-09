using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

public class TemplateRequestFailedException : InternalServerException
{
    public const string DefaultMessage = "Request could not be processed.";

    public TemplateRequestFailedException() : base(DefaultMessage) { }

    public TemplateRequestFailedException(Exception ex) : base(DefaultMessage, ex) { }
}

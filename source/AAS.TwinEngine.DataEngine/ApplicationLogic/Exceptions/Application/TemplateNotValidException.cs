using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

public class TemplateNotValidException : InternalServerException
{
    public const string DefaultMessage = "Template Not Valid";

    public TemplateNotValidException() : base(DefaultMessage) { }

    public TemplateNotValidException(Exception ex) : base(DefaultMessage, ex) { }
}

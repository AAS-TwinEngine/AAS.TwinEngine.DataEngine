using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

public class InternalDataProcessingException : InternalServerException
{
    public const string DefaultMessage = "Internal Server Error.";

    public InternalDataProcessingException() : base(DefaultMessage) { }

    public InternalDataProcessingException(Exception ex) : base(DefaultMessage, ex) { }
}

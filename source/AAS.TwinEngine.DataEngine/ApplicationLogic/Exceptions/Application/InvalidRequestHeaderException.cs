using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

public class InvalidRequestHeaderException : BadRequestException
{
    public const string DefaultMessage = "Invalid request Header.";

    public InvalidRequestHeaderException() : base(DefaultMessage) { }

    public InvalidRequestHeaderException(Exception ex) : base(DefaultMessage, ex) { }
}

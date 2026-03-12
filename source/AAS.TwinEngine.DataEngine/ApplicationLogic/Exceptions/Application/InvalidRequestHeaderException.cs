using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

public class InvalidRequestHeaderException : BadRequestException
{
    public const string DefaultMessage = "Invalid Request Header.";

    public InvalidRequestHeaderException() : base(DefaultMessage) { }

    public InvalidRequestHeaderException(string message) : base(message) { }

    public InvalidRequestHeaderException(Exception ex) : base(DefaultMessage, ex) { }
}

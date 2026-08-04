using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

public class InvalidFileUrlException : BadRequestException
{
    public const string DefaultMessage = "Invalid file attachment URL protocol.";

    public InvalidFileUrlException() : base(DefaultMessage) { }

    public InvalidFileUrlException(Exception ex) : base(DefaultMessage, ex) { }
}

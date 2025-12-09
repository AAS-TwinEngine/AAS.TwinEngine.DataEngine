using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

public class InvalidUserInputException : BadRequestException
{
    public const string DefaultMessage = "Invalid User Input.";

    public InvalidUserInputException() : base(DefaultMessage) { }

    public InvalidUserInputException(Exception ex) : base(DefaultMessage, ex) { }
}

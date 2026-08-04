using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

public class InvalidSubmodelElementTypeException : BadRequestException
{
    public const string DefaultMessage = "The Submodel Element type is inavalid.";

    public InvalidSubmodelElementTypeException() : base(DefaultMessage) { }

    public InvalidSubmodelElementTypeException(Exception ex) : base(DefaultMessage, ex) { }
}

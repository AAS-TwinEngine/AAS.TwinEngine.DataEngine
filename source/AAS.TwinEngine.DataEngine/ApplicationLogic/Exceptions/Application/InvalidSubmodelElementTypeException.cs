using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

/// <summary>
/// Thrown when the SubmodelElement at the requested idShortPath is not a File element.
/// Maps to HTTP 400 Bad Request via <see cref="BadRequestException"/>.
/// </summary>
public class InvalidSubmodelElementTypeException : BadRequestException
{
    public const string DefaultMessage = "The Submodel Element type.";

    public InvalidSubmodelElementTypeException() : base(DefaultMessage) { }

    public InvalidSubmodelElementTypeException(Exception ex) : base(DefaultMessage, ex) { }
}

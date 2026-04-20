using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

public class InvalidDependencyException : InternalServerException
{
    public const string DefaultMessage = "An unexpected error occurred.";

    public InvalidDependencyException(Exception ex) : base(DefaultMessage, ex) { }

    public InvalidDependencyException(string parameter, ILogger? logger = null) : base(DefaultMessage) => logger?.LogError("Dependency failure detected: {Parameter}", parameter);
}

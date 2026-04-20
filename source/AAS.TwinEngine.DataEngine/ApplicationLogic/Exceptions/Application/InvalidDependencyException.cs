using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

public class InvalidDependencyException : InternalServerException
{
    public const string DefaultMessage = "Invalid Dependency";

    public InvalidDependencyException() : base(DefaultMessage) { }

    public InvalidDependencyException(Exception ex) : base(DefaultMessage, ex) { }

    public InvalidDependencyException(string parameter, ILogger? logger = null) : base(DefaultMessage) => logger?.LogError("Invalid dependency: {Parameter}", parameter);
}


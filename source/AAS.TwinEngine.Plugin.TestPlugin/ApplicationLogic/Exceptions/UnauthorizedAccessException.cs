namespace Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Exceptions;

public class UnauthorizedAccessException : Exception
{
    public UnauthorizedAccessException(string message)
        : base(message)
    {
    }

    public UnauthorizedAccessException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public UnauthorizedAccessException(string name, string identifier)
        : this($"You are not Authorize to access {name} with this identifier {identifier}")
    {
    }

    public UnauthorizedAccessException()
    {
    }
}
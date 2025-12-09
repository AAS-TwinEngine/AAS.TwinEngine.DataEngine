namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;

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

    public UnauthorizedAccessException()
    {
    }
}

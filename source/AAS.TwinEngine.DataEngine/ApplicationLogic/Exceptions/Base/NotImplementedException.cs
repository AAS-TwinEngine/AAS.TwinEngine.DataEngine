namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;

public class NotImplementedException : Exception
{
    public NotImplementedException()
    {
    }

    public NotImplementedException(string message)
        : base(message)
    {
    }

    public NotImplementedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

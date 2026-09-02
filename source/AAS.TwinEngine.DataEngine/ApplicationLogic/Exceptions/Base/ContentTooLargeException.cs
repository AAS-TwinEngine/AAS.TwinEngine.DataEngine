namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;

public class ContentTooLargeException : Exception
{
    public ContentTooLargeException(string message)
        : base(message)
    {
    }

    public ContentTooLargeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public ContentTooLargeException()
    {
    }
}

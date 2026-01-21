namespace AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Exceptions;

public class InternalServerException : Exception
{
    public InternalServerException(string message)
        : base(message)
    {
    }

    public InternalServerException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public InternalServerException()
    {
    }

    public InternalServerException(string message, string title)
        : base(message)
    {
        Title = title;
    }

    public string? Title { get; }
}
namespace AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException()
    {
    }

    public NotFoundException(string message)
        : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public NotFoundException(string message, string title)
        : base(message)
    {
        Title = title;
    }

    public string? Title { get; }
}
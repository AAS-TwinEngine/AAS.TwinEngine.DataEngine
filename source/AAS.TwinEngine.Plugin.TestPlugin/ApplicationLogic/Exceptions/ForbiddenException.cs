namespace Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Exceptions;

public class ForbiddenException : Exception
{
    public ForbiddenException()
    {
    }

    public ForbiddenException(string message)
        : base(message)
    {
    }

    public ForbiddenException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public ForbiddenException(string message, string title)
        : base(message)
    {
        Title = title;
    }

    public string? Title { get; }
}
namespace Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Exceptions;

public class BadRequestException : Exception
{
    public BadRequestException()
    {
    }

    public BadRequestException(string message)
        : base(message)
    {
    }

    public BadRequestException(int? errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public BadRequestException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public BadRequestException(string message, string title)
        : base(message)
    {
        Title = title;
    }

    public string? Title { get; }
    public int? ErrorCode { get; }
}

namespace AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Exceptions;

public class CustomException : Exception
{
    public CustomException()
    {
    }

    public CustomException(string message)
        : base(message)
    {
    }

    public CustomException(int? errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public CustomException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public CustomException(string message, string title)
        : base(message)
    {
        Title = title;
    }

    public string? Title { get; }
    public int? ErrorCode { get; }
}

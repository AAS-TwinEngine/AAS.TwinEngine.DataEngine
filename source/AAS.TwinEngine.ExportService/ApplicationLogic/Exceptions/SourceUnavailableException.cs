namespace AAS.TwinEngine.ExportService.ApplicationLogic.Exceptions;

public class SourceUnavailableException : Exception
{
    public SourceUnavailableException()
    {
    }

    public SourceUnavailableException(string message)
        : base(message)
    {
    }

    public SourceUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

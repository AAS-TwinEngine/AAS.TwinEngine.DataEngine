namespace AAS.TwinEngine.ExportService.ApplicationLogic.Exceptions;

/// <summary>
/// Thrown when the exporter cannot obtain a valid access token for an endpoint.
/// </summary>
public sealed class AuthenticationFailedException : Exception
{
    public AuthenticationFailedException(string message) : base(message) { }
    public AuthenticationFailedException(string message, Exception inner) : base(message, inner) { }
}

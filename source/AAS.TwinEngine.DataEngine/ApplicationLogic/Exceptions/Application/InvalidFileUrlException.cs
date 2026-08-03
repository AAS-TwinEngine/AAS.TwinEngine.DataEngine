using System.Globalization;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

/// <summary>
/// Thrown when the file attachment URL is invalid or cannot be used to retrieve the file.
/// Maps to HTTP 400 Bad Request via <see cref="BadRequestException"/>.
/// </summary>
public class InvalidFileUrlException : BadRequestException
{
    public const string DefaultMessage = "Invalid file attachment URL protocol.";

    public InvalidFileUrlException() : base(DefaultMessage) { }

    public InvalidFileUrlException(Exception ex) : base(DefaultMessage, ex) { }
}
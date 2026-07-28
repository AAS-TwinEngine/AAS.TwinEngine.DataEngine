using System.Globalization;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

/// <summary>
/// Thrown when the file attachment at the requested idShortPath exceeds the configured maximum file size.
/// Maps to HTTP 400 Bad Request via <see cref="BadRequestException"/>.
/// </summary>
public class FileSizeExceededException : BadRequestException
{
    public const string MessageTemplate = "The file attachment at path '{0}' exceeds the maximum allowed size of {1} bytes (actual: {2} bytes).";

    public FileSizeExceededException(string idShortPath, long actualBytes, long maxBytes)
        : base(string.Format(CultureInfo.InvariantCulture, MessageTemplate, idShortPath, maxBytes, actualBytes)) { }
}

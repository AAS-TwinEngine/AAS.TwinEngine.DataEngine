using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

public class FileSizeExceededException : BadRequestException
{
    public const string DefaultMessage = "The file attachment exceeds the maximum allowed size.";

    public FileSizeExceededException() : base(DefaultMessage) { }

    public FileSizeExceededException(Exception ex) : base(DefaultMessage, ex) { }
}

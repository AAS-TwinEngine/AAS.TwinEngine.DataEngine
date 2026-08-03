using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

public class ThumbnailNotFoundException : NotFoundException
{
    public const string ServiceName = "Thumbnail";

    public ThumbnailNotFoundException() : base(ServiceName) { }
    public ThumbnailNotFoundException(string aasId) : base(ServiceName, aasId) { }
    public ThumbnailNotFoundException(Exception ex) : base(ServiceName, ex) { }
    public ThumbnailNotFoundException(Exception ex, string aasId) : base(ServiceName, aasId, ex) { }
}

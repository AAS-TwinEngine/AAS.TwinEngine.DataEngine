using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

public class AssetInformationNotFoundException : NotFoundException
{
    public const string ServiceName = "Asset Information";

    public AssetInformationNotFoundException() : base(ServiceName) { }
    public AssetInformationNotFoundException(string aasId) : base(ServiceName, aasId) { }
    public AssetInformationNotFoundException(Exception ex) : base(ServiceName, ex) { }
    public AssetInformationNotFoundException(Exception ex, string aasId) : base(ServiceName, aasId, ex) { }
}

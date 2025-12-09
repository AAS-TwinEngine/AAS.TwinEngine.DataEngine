using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

public class SubmodelNotFoundException : NotFoundException
{
    public const string ServiceName = "Submodel";

    public SubmodelNotFoundException() : base(ServiceName) { }
    public SubmodelNotFoundException(string submodelId) : base(ServiceName, submodelId) { }
    public SubmodelNotFoundException(Exception ex) : base(ServiceName, ex) { }
    public SubmodelNotFoundException(Exception ex, string submodelId) : base(ServiceName, submodelId, ex) { }
}

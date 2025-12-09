using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

public class SubmodelElementNotFoundException : NotFoundException
{
    public const string ServiceName = "Submodel Element";

    public SubmodelElementNotFoundException() : base(ServiceName) { }
    public SubmodelElementNotFoundException(string submodelId) : base(ServiceName, submodelId) { }
    public SubmodelElementNotFoundException(Exception ex) : base(ServiceName, ex) { }
    public SubmodelElementNotFoundException(Exception ex, string submodelId) : base(ServiceName, submodelId, ex) { }
}

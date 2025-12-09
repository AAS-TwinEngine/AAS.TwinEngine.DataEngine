using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

public class SubmodelDescriptorNotFoundException : NotFoundException
{
    public const string ServiceName = "Submodel Descriptor";

    public SubmodelDescriptorNotFoundException() : base(ServiceName) { }
    public SubmodelDescriptorNotFoundException(string submodelId) : base(ServiceName, submodelId) { }
    public SubmodelDescriptorNotFoundException(Exception ex) : base(ServiceName, ex) { }
    public SubmodelDescriptorNotFoundException(Exception ex, string submodelId) : base(ServiceName, submodelId, ex) { }
}

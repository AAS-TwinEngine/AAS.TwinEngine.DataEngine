using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

public class ShellDescriptorNotFoundException : NotFoundException
{
    public const string ServiceName = "Shell Descriptor";

    public ShellDescriptorNotFoundException() : base(ServiceName) { }
    public ShellDescriptorNotFoundException(string submodelId) : base(ServiceName, submodelId) { }
    public ShellDescriptorNotFoundException(Exception ex) : base(ServiceName, ex) { }
    public ShellDescriptorNotFoundException(Exception ex, string submodelId) : base(ServiceName, submodelId, ex) { }
}

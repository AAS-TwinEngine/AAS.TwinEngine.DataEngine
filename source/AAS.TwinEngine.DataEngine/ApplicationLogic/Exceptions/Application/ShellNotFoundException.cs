using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

public class ShellNotFoundException : NotFoundException
{
    public const string ServiceName = "Shell";

    public ShellNotFoundException() : base(ServiceName) { }
    public ShellNotFoundException(string submodelId) : base(ServiceName, submodelId) { }
    public ShellNotFoundException(Exception ex) : base(ServiceName, ex) { }
    public ShellNotFoundException(Exception ex, string submodelId) : base(ServiceName, submodelId, ex) { }
}

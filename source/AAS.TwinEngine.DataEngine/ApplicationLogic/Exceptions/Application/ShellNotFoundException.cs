using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

public class ShellNotFoundException : NotFoundException
{
    public const string ServiceName = "shell";

    public ShellNotFoundException() : base(ServiceName) { }
    public ShellNotFoundException(string aasId) : base(ServiceName, aasId) { }
    public ShellNotFoundException(Exception ex) : base(ServiceName, ex) { }
    public ShellNotFoundException(Exception ex, string aasId) : base(ServiceName, aasId, ex) { }
}

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

public class RegistryNotAvailableException : ServiceUnavailableException
{
    public const string ServiceName = "Registry";

    public RegistryNotAvailableException() : base(ServiceName) { }

    public RegistryNotAvailableException(Exception ex) : base(ServiceName, ex) { }
}

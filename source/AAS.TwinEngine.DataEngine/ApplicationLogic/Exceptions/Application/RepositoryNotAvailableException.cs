using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

public class RepositoryNotAvailableException : ServiceUnavailableException
{
    public const string ServiceName = "Repository";

    public RepositoryNotAvailableException() : base(ServiceName) { }

    public RepositoryNotAvailableException(Exception ex) : base(ServiceName, ex) { }
}

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

public class ServiceUnAuthorizedException : Base.UnauthorizedAccessException
{
    public const string DefaultMessage = "Access to the requested service is unauthorized.";

    public ServiceUnAuthorizedException() : base(DefaultMessage) { }

    public ServiceUnAuthorizedException(Exception ex) : base(DefaultMessage, ex) { }
}

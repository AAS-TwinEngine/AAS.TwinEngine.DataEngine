namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;

public class NotFoundException : Exception
{
    public NotFoundException()
        : base("Requested item is not found.")
    {
    }

    public NotFoundException(string serviceName)
        : base($"{serviceName} not found.")
    {
    }

    public NotFoundException(string serviceName, string id)
        : base($"{serviceName} not found. ID : {id}")
    {
    }

    public NotFoundException(string serviceName, Exception innerException)
        : base($"{serviceName} not found.", innerException)
    {
    }

    public NotFoundException(string serviceName, string id, Exception innerException)
        : base($"{serviceName} not found. ID : {id}", innerException)
    {
    }
}

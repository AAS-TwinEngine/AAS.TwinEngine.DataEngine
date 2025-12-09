namespace AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;

public interface ICreateClient
{
    HttpClient CreateClient(string clientName);
}

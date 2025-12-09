namespace AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;

public class HttpClientFactory(IHttpClientFactory httpClientFactory) : ICreateClient
{
    public HttpClient CreateClient(string clientName) => httpClientFactory.CreateClient(clientName);
}

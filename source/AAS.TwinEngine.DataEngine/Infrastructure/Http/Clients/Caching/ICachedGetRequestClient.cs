namespace AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients.Caching;

public interface ICachedGetRequestClient
{
    Task<string> GetStringAsync(string relativeUrl, string httpClientName, int expirationTime, CancellationToken cancellationToken);
}

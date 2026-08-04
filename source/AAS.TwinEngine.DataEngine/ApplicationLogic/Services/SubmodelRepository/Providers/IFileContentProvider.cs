namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.Providers;

public interface IFileContentProvider
{
    Task<HttpResponseMessage> GetResponseHeadersAsync(string fileUrl, CancellationToken cancellationToken);

    Task<Stream> ReadStreamAsync(HttpResponseMessage response, CancellationToken cancellationToken);
}

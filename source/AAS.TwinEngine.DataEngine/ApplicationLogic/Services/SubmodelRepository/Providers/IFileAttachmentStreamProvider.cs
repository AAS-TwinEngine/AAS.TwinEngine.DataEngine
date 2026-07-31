namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.Providers;

public interface IFileAttachmentStreamProvider
{
    Task<HttpResponseMessage> GetResponseHeadersAsync(string fileUrl, CancellationToken cancellationToken);

    Task<Stream> ReadStreamAsync(HttpResponseMessage response, CancellationToken cancellationToken);
}

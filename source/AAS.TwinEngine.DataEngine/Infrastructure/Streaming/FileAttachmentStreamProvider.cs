using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.Providers;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Streaming;

public class FileAttachmentStreamProvider(IHttpClientFactory httpClientFactory) : IFileAttachmentStreamProvider
{
    public async Task<HttpResponseMessage> GetResponseHeadersAsync(string fileUrl, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(HttpClientNames.FileAttachmentProvider);
        return await client.GetAsync(new Uri(fileUrl), HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Stream> ReadStreamAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        return await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
    }
}

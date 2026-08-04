using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.Providers;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Streaming;

public class FileContentProvider(IHttpClientFactory httpClientFactory) : IFileContentProvider
{
    public async Task<FileContentResponse> GetFileContentAsync(string fileUrl, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(HttpClientNames.FileAttachmentProvider);
        var response = await client.GetAsync(new Uri(fileUrl), HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

        try
        {
            _ = response.EnsureSuccessStatusCode();

            var contentLength = response.Content.Headers.ContentLength;
            var contentType = response.Content.Headers.ContentType?.ToString();
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

            return new FileContentResponse(stream, contentLength, contentType)
            {
                OnDispose = response.Dispose
            };
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }
}

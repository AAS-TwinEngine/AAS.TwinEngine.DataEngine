using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.Providers;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.FileContentProvider.Services;

public class FileContentProvider(ICreateClient httpClientFactory, IOptions<GeneralConfig> generalConfig) : IFileContentProvider
{
    private readonly long _maxFileAttachmentSizeBytes = generalConfig.Value.MaxFileAttachmentSizeBytes;
    public async Task<FileContentResponse> GetFileContentAsync(string fileUrl, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(HttpClientNames.FileAttachmentProvider);
        var response = await client.GetAsync(new Uri(fileUrl), HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

        try
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new InternalDataProcessingException();
            }

            var contentLength = response.Content.Headers.ContentLength;
            if (contentLength.HasValue && contentLength.Value > _maxFileAttachmentSizeBytes)
            {
                throw new FileSizeExceededException();
            }
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

            return new FileContentResponse(stream)
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

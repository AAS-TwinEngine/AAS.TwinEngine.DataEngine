using AAS.TwinEngine.DataEngine.DomainModel.Shared;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.Providers;

public interface IFileContentProvider
{
    Task<FileContentResponse> GetFileContentAsync(string fileUrl, CancellationToken cancellationToken);
}

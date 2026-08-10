using System.Text.Json;

using AAS.TwinEngine.DataEngine.Api.AasRepository.Requests;
using AAS.TwinEngine.DataEngine.Api.AasRepository.Responses;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Responses;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.Api.AasRepository.Handler;

public interface IAasRepositoryHandler
{
    Task<ShellsDto> GetShellsByAssetIdsAsync(GetShellsByAssetIdsRequest request, CancellationToken cancellationToken);

    Task<IAssetAdministrationShell> GetShellByIdAsync(GetShellRequest request, CancellationToken cancellationToken);

    Task<IAssetInformation> GetAssetInformationByIdAsync(GetAssetInformationRequest request, CancellationToken cancellationToken);

    Task<JsonElement> GetSubmodelRefByIdAsync(GetSubmodelRefRequest request, CancellationToken cancellationToken);

    Task<ISubmodel> GetSubmodelByAasIdAsync(GetSubmodelByAasRequest request, CancellationToken cancellationToken);

    Task<SubmodelElementsDto> GetAllSubmodelElementsByAasIdAsync(GetAllSubmodelElementsByAasRequest request, CancellationToken cancellationToken);

    Task<ISubmodelElement> GetSubmodelElementByAasIdAsync(GetSubmodelElementByAasRequest request, CancellationToken cancellationToken);

    Task<FileAttachmentResult> GetFileByPathByAasIdAsync(GetFileByPathByAasIdRequest request, CancellationToken cancellationToken);
}

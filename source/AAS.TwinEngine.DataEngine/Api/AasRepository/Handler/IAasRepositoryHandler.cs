using System.Text.Json;

using AAS.TwinEngine.DataEngine.Api.AasRepository.Requests;
using AAS.TwinEngine.DataEngine.Api.AasRepository.Responses;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.Api.AasRepository.Handler;

public interface IAasRepositoryHandler
{
    Task<ShellsDto> GetShellsByAssetIdsAsync(string[]? assetIds, string? idShort, int? limit, string? cursor, CancellationToken cancellationToken);

    Task<IAssetAdministrationShell> GetShellByIdAsync(GetShellRequest request, CancellationToken cancellationToken);

    Task<IAssetInformation> GetAssetInformationByIdAsync(GetAssetInformationRequest request, CancellationToken cancellationToken);

    Task<JsonElement> GetSubmodelRefByIdAsync(GetSubmodelRefRequest request, CancellationToken cancellationToken);
}

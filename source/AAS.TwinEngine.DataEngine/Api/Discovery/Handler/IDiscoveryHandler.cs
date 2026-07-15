using AAS.TwinEngine.DataEngine.Api.Discovery.Requests;
using AAS.TwinEngine.DataEngine.Api.Discovery.Responses;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.Api.Discovery.Handler;

public interface IDiscoveryHandler
{
    Task<ShellsByAssetLinkResponseDto> SearchShellsByAssetLinkAsync(SearchShellsByAssetLinkRequest request, CancellationToken cancellationToken);

    Task<IList<ISpecificAssetId>> GetSpecificAssetIdByAasIdentifierAsync(GetSpecificAssetIdByAasIdentifierRequest request, CancellationToken cancellationToken);
}

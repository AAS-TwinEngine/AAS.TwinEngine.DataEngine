using AAS.TwinEngine.DataEngine.Api.Discovery.Responses;
using AAS.TwinEngine.DataEngine.DomainModel.Discovery;

namespace AAS.TwinEngine.DataEngine.Api.Discovery.Handler;

public interface IDiscoveryHandler
{
    Task<ShellsByAssetLinkResponseDto> SearchShellsByAssetLinkAsync(
        AssetLink[] assetLinks, int? limit, string? cursor, CancellationToken cancellationToken);
}

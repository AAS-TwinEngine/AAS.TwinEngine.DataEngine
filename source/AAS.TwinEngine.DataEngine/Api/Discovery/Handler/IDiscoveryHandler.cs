using AAS.TwinEngine.DataEngine.Api.Discovery.Requests;
using AAS.TwinEngine.DataEngine.Api.Discovery.Responses;

namespace AAS.TwinEngine.DataEngine.Api.Discovery.Handler;

public interface IDiscoveryHandler
{
    Task<ShellsByAssetLinkResponseDto> SearchShellsByAssetLinkAsync(
        AssetLinkDto[] assetLinks, int? limit, string? cursor, CancellationToken cancellationToken);
}

using AAS.TwinEngine.DataEngine.DomainModel.Discovery;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Discovery;

public interface IAssetIdSearchService
{
    Task<ShellsByAssetLink> SearchShellsByAssetLinkAsync(IList<AssetLink> assetLinks, int? limit, string? cursor, CancellationToken cancellationToken);
}

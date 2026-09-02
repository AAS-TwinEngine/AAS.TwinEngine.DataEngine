using AAS.TwinEngine.DataEngine.DomainModel.Discovery;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Discovery;

public interface IAssetIdSearchService
{
    Task<ShellsByAssetLink> SearchShellsByAssetLinkAsync(IList<AssetLink> assetLinks, int limit, string? cursor, CancellationToken cancellationToken);

    Task<IList<ISpecificAssetId>> GetSpecificAssetIdByAasIdentifierAsync(string aasIdentifier, CancellationToken cancellationToken);
}

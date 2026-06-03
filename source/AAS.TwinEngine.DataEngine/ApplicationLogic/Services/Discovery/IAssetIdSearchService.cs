using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.Discovery;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Discovery;

public interface IAssetIdSearchService
{
    Task<(IList<string> AasIds, DomainModel.Shared.PagingMetaData PagingMetaData)> SearchShellsByAssetLinkAsync(
        IList<AssetLink> assetLinks, int? limit, string? cursor, CancellationToken cancellationToken);

    Task<(IList<ShellDescriptorMetaData> Metadata, DomainModel.Shared.PagingMetaData PagingMetaData)> GetShellMetadataByAssetIdsAsync(
        IList<SpecificAssetIdFilter> assetIds, int? limit, string? cursor, CancellationToken cancellationToken);
}

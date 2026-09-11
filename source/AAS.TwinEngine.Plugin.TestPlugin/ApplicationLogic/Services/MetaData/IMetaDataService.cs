using AAS.TwinEngine.Plugin.TestPlugin.DomainModel.MetaData;

namespace AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.MetaData;

public interface IMetaDataService
{
    Task<ShellDescriptorsData> GetShellDescriptorsAsync(
        int? limit,
        string? cursor,
        AssetIdFilterHeader? filter,
        string? idShort,
        string? assetKind,
        string? assetType,
        CancellationToken cancellationToken);

    Task<ShellDescriptorData> GetShellDescriptorAsync(string aasIdentifier, CancellationToken cancellationToken);

    Task<AssetData> GetAssetAsync(string assetIdentifier, CancellationToken cancellationToken);
}

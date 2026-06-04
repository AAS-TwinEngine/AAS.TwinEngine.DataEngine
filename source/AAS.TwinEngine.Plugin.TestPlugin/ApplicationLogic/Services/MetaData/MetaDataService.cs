using AAS.TwinEngine.Plugin.TestPlugin.DomainModel.MetaData;

namespace AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.MetaData;

public class MetaDataService(IMetaDataProvider metaDataProvider) : IMetaDataService
{
    public async Task<ShellDescriptorsData> GetShellDescriptorsAsync(int? limit, string? cursor, AssetIdFilterHeader? filter, CancellationToken cancellationToken) => await metaDataProvider.GetShellDescriptorsAsync(limit, cursor, filter, cancellationToken);

    public async Task<ShellDescriptorData> GetShellDescriptorAsync(string aasIdentifier, CancellationToken cancellationToken) => await metaDataProvider.GetShellDescriptorAsync(aasIdentifier, cancellationToken);

    public async Task<AssetData> GetAssetAsync(string assetIdentifier, CancellationToken cancellationToken) => await metaDataProvider.GetAssetAsync(assetIdentifier, cancellationToken);
}

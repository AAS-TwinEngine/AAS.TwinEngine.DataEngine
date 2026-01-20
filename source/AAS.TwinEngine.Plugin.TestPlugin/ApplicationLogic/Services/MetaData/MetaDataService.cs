using Aas.TwinEngine.Plugin.TestPlugin.DomainModel.MetaData;

namespace Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.MetaData;

public class MetaDataService(
    ILogger<MetaDataService> logger,
    IMetaDataProvider metaDataProvider) : IMetaDataService
{
    public async Task<ShellDescriptorsData> GetShellDescriptorsAsync(int? limit, string? cursor, CancellationToken cancellationToken) => await metaDataProvider.GetShellDescriptorsAsync(limit, cursor, cancellationToken);

    public async Task<ShellDescriptorData> GetShellDescriptorAsync(string aasIdentifier, CancellationToken cancellationToken) => await metaDataProvider.GetShellDescriptorAsync(aasIdentifier, cancellationToken);

    public async Task<AssetData> GetAssetAsync(string assetIdentifier, CancellationToken cancellationToken) => await metaDataProvider.GetAssetAsync(assetIdentifier, cancellationToken);
}

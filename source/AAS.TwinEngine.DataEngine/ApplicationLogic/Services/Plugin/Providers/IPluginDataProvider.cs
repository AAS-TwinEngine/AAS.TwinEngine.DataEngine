using AAS.TwinEngine.DataEngine.DomainModel.Plugin;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Providers;

public interface IPluginDataProvider
{
    Task<IList<string>> GetDataForSemanticIdsAsync(IList<PluginRequestSubmodel> pluginRequests, string submodelId, CancellationToken cancellationToken);

    Task<IList<string>> GetDataForAllShellDescriptorsAsync(int limit, string? cursor, AssetKind? assetKind, string? assetType, IList<PluginRequestMetaData> pluginRequests, CancellationToken cancellationToken);

    Task<IList<string>> GetDataForShellDescriptorByIdAsync(IList<PluginRequestMetaData> pluginRequests, CancellationToken cancellationToken);

    Task<IList<string>> GetDataForAssetInformationByIdAsync(IList<PluginRequestMetaData> pluginRequests, CancellationToken cancellationToken);

    Task<IList<string>> GetDataForShellDescriptorsByAssetIdsAsync(IList<PluginRequestMetaData> pluginRequests, string? assetIdsHeaderValue, string? idShortHeaderValue, int limit, string? cursor, CancellationToken cancellationToken);
}

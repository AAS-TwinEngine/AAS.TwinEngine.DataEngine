using AAS.TwinEngine.DataEngine.DomainModel.Plugin;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Providers;

public interface IPluginDataProvider
{
    Task<IList<string>> GetDataForSemanticIdsAsync(IList<PluginRequestSubmodel> pluginRequest, string submodelId, CancellationToken cancellationToken);

    Task<IList<string>> GetDataForAllShellDescriptorsAsync(int? limit, string? cursor, IList<PluginRequestMetaData> pluginRequests, CancellationToken cancellationToken);

    Task<IList<string>> GetDataForShellDescriptorByIdAsync(IList<PluginRequestMetaData> pluginRequests, CancellationToken cancellationToken);

    Task<IList<string>> GetDataForAssetInformationByIdAsync(IList<PluginRequestMetaData> pluginRequests, CancellationToken cancellationToken);

    Task<IList<string>> GetDataForShellDescriptorsByAssetIdsAsync(IList<PluginRequestMetaData> pluginRequests, string? assetIdsHeaderValue, string? idShortHeaderValue, CancellationToken cancellationToken);
}

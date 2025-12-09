using AAS.TwinEngine.DataEngine.DomainModel.Plugin;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Providers;

public interface IPluginDataProvider
{
    Task<IList<HttpContent>> GetDataForSemanticIdsAsync(IList<PluginRequestSubmodel> pluginRequest, string submodelId, CancellationToken cancellationToken);

    Task<IList<HttpContent>> GetDataForAllShellDescriptorsAsync(int? limit, string? cursor, IList<PluginRequestMetaData> pluginRequests, CancellationToken cancellationToken);

    Task<IList<HttpContent>> GetDataForShellDescriptorByIdAsync(IList<PluginRequestMetaData> pluginRequests, CancellationToken cancellationToken);

    Task<IList<HttpContent>> GetDataForAssetInformationByIdAsync(IList<PluginRequestMetaData> pluginRequests, CancellationToken cancellationToken);
}

using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.AasRepository;
using AAS.TwinEngine.DataEngine.DomainModel.Plugin;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;

/// <summary>
/// Provides a single interface for the application to interact with the plugins.
/// Contains the functionality of a plugin registry.
/// </summary>
public interface IPluginDataHandler
{
    Task<SemanticTreeNode> TryGetValuesAsync(IReadOnlyList<PluginManifest> pluginManifests, SemanticTreeNode semanticIds, string submodelId, CancellationToken cancellationToken);

    Task<ShellDescriptorsMetaData> GetDataForAllShellDescriptorsAsync(int? limit, string? cursor, IReadOnlyList<PluginManifest> pluginManifests, CancellationToken cancellationToken);

    Task<ShellDescriptorMetaData> GetDataForShellDescriptorAsync(IReadOnlyList<PluginManifest> pluginManifests, string id, CancellationToken cancellationToken);

    Task<AssetData> GetDataForAssetInformationByIdAsync(IReadOnlyList<PluginManifest> pluginManifests, string id, CancellationToken cancellationToken);
}

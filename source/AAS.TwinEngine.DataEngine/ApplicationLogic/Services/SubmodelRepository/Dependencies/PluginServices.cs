using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.Dependencies;

/// <summary>
/// Groups plugin-related submodel repository dependencies.
/// </summary>
public class PluginServices(
    IPluginDataHandler pluginDataHandler,
    IPluginManifestConflictHandler pluginManifestConflictHandler)
{
    /// <summary>
    /// Handles plugin data retrieval.
    /// </summary>
    public IPluginDataHandler PluginDataHandler { get; } = pluginDataHandler;

    /// <summary>
    /// Resolves plugin manifest conflicts and exposes active manifests.
    /// </summary>
    public IPluginManifestConflictHandler PluginManifestConflictHandler { get; } = pluginManifestConflictHandler;
}
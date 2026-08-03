using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.Dependencies;

/// <summary>
/// Groups plugin-related submodel repository dependencies.
/// </summary>
public class PluginServices(
    IPluginDataHandler pluginDataHandler,
    IPluginManifestConflictHandler pluginManifestConflictHandler)
{
    public IPluginDataHandler PluginDataHandler { get; } = pluginDataHandler;

    public IPluginManifestConflictHandler PluginManifestConflictHandler { get; } = pluginManifestConflictHandler;
}
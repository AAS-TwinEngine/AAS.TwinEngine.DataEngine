using AAS.TwinEngine.DataEngine.DomainModel.Plugin;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;

public interface IPluginManifestConflictHandler
{
    IReadOnlyList<PluginManifest> Manifests { get; }

    Task ProcessManifests(IList<PluginManifest> manifests);
}

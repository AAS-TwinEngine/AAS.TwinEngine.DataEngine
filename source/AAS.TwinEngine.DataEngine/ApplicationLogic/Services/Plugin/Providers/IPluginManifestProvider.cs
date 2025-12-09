using AAS.TwinEngine.DataEngine.DomainModel.Plugin;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Providers;

public interface IPluginManifestProvider
{
    Task<IList<PluginManifest>> GetAllPluginManifestsAsync(CancellationToken cancellationToken);
}

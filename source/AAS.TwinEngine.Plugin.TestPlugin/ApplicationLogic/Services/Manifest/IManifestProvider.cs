using Aas.TwinEngine.Plugin.TestPlugin.DomainModel.Manifest;

namespace Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.Manifest;

public interface IManifestProvider
{
    public ManifestData GetManifestData();
}

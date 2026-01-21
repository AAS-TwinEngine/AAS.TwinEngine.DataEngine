using AAS.TwinEngine.Plugin.TestPlugin.DomainModel.Manifest;

namespace AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.Manifest;

public interface IManifestProvider
{
    public ManifestData GetManifestData();
}

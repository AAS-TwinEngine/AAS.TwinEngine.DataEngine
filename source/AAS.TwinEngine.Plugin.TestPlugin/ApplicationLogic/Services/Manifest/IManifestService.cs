using AAS.TwinEngine.Plugin.TestPlugin.DomainModel.Manifest;

namespace AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.Manifest;

public interface IManifestService
{
    public Task<ManifestData> GetManifestData(CancellationToken cancellationToken);
}

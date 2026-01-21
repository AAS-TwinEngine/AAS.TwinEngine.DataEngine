using AAS.TwinEngine.Plugin.TestPlugin.DomainModel.Manifest;

namespace AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.Manifest;

public class ManifestService(IManifestProvider manifestProvider) : IManifestService
{
    public Task<ManifestData> GetManifestData(CancellationToken cancellationToken) => Task.FromResult(manifestProvider.GetManifestData());
}

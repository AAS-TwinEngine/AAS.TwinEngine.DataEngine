using AAS.TwinEngine.Plugin.TestPlugin.Api.Manifest.Responses;

namespace AAS.TwinEngine.Plugin.TestPlugin.Api.Manifest.Handler;

public interface IManifestHandler
{
    Task<ManifestDto> GetManifestData(CancellationToken cancellationToken);
}

using Aas.TwinEngine.Plugin.TestPlugin.Api.Manifest.Responses;

namespace Aas.TwinEngine.Plugin.TestPlugin.Api.Manifest.Handler;

public interface IManifestHandler
{
    Task<ManifestDto> GetManifestData(CancellationToken cancellationToken);
}

using AAS.TwinEngine.Plugin.TestPlugin.Api.Manifest.MappingProfiles;
using AAS.TwinEngine.Plugin.TestPlugin.Api.Manifest.Responses;
using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.Manifest;

namespace AAS.TwinEngine.Plugin.TestPlugin.Api.Manifest.Handler;

public class ManifestHandler(ILogger<ManifestHandler> logger,
                             IManifestService manifestService) : IManifestHandler
{
    public async Task<ManifestDto> GetManifestData(CancellationToken cancellationToken)
    {
        logger.LogInformation("Start executing request for manifest data");

        var manifest = await manifestService.GetManifestData(cancellationToken);

        return manifest.ToDto();
    }
}

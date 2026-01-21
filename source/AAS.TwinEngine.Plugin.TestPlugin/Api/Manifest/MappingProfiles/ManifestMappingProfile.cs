using AAS.TwinEngine.Plugin.TestPlugin.Api.Manifest.Responses;
using AAS.TwinEngine.Plugin.TestPlugin.DomainModel.Manifest;

namespace AAS.TwinEngine.Plugin.TestPlugin.Api.Manifest.MappingProfiles;

public static class ManifestMappingProfile
{
    public static ManifestDto ToDto(this ManifestData? data)
    {
        return new ManifestDto()
        {
            Capabilities = new CapabilitiesDto()
            {
                HasAssetInformation = data.Capabilities.HasAssetInformation,
                HasShellDescriptor = data.Capabilities.HasShellDescriptor
            },
            SupportedSemanticIds = data.SupportedSemanticIds
        };
    }
}

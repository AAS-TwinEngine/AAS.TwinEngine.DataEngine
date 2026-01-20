using Aas.TwinEngine.Plugin.TestPlugin.Api.Manifest.MappingProfiles;
using Aas.TwinEngine.Plugin.TestPlugin.DomainModel.Manifest;

namespace Aas.TwinEngine.Plugin.TestPlugin.UnitTests.Api.Manifest.MappingProfiles;

public class ManifestMappingProfileTests
{
    [Fact]
    public void ToDto_ShouldMapManifestDataToManifestDtoCorrectly()
    {
        var manifestData = new ManifestData
        {
            Capabilities = new CapabilitiesData
            {
                HasAssetInformation = true,
                HasShellDescriptor = false
            },
            SupportedSemanticIds = ["semantic1", "semantic2"]
        };

        var result = manifestData.ToDto();

        Assert.NotNull(result);
        Assert.NotNull(result.Capabilities);
        Assert.Equal(manifestData.Capabilities.HasAssetInformation, result.Capabilities.HasAssetInformation);
        Assert.Equal(manifestData.Capabilities.HasShellDescriptor, result.Capabilities.HasShellDescriptor);
        Assert.Equal(manifestData.SupportedSemanticIds, result.SupportedSemanticIds);
    }

    [Fact]
    public void ToDto_ShouldThrowException_WhenManifestDataIsNull()
    {
        ManifestData? manifestData = null;

        Assert.Throws<NullReferenceException>(() => manifestData!.ToDto());
    }
}

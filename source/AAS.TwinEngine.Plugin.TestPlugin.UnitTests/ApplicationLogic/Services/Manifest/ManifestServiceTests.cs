using Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.Manifest;
using Aas.TwinEngine.Plugin.TestPlugin.DomainModel.Manifest;

using NSubstitute;

namespace Aas.TwinEngine.Plugin.TestPlugin.UnitTests.ApplicationLogic.Services.Manifest;

public class ManifestServiceTests
{
    private readonly IManifestProvider _manifestProvider = Substitute.For<IManifestProvider>();
    private readonly ManifestService _sut;

    public ManifestServiceTests() => _sut = new ManifestService(_manifestProvider);

    [Fact]
    public async Task GetManifestData_ShouldReturnManifestData_FromProvider()
    {
        var expectedManifest = new ManifestData
        {
            Capabilities = new CapabilitiesData
            {
                HasAssetInformation = true,
                HasShellDescriptor = false
            },
            SupportedSemanticIds = ["semantic1"]
        };
        _manifestProvider.GetManifestData().Returns(expectedManifest);

        var result = await _sut.GetManifestData(CancellationToken.None);

        Assert.Equal(expectedManifest, result);
        Assert.Equal(expectedManifest.Capabilities.HasAssetInformation, result.Capabilities.HasAssetInformation);
        Assert.Equal(expectedManifest.Capabilities.HasShellDescriptor, result.Capabilities.HasShellDescriptor);
        Assert.Equal(expectedManifest.SupportedSemanticIds, result.SupportedSemanticIds);
    }
}

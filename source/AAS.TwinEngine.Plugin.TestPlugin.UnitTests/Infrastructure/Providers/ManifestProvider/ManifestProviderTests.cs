using System.Text.Json;

using Aas.TwinEngine.Plugin.TestPlugin.Infrastructure.Providers;
using Aas.TwinEngine.Plugin.TestPlugin.Infrastructure.Providers.Config;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using Provider = Aas.TwinEngine.Plugin.TestPlugin.Infrastructure.Providers.ManifestProvider.ManifestProvider;

namespace Aas.TwinEngine.Plugin.TestPlugin.UnitTests.Infrastructure.Providers.ManifestProvider;

public class ManifestProviderTests
{
    private readonly ILogger<Provider> _logger;
    private readonly Provider _sut;

    public ManifestProviderTests()
    {
        _logger = Substitute.For<ILogger<Provider>>();
        var capabilities = Substitute.For<IOptions<Capabilities>>();
        capabilities.Value.Returns(new Capabilities { HasAssetInformation = true, HasShellDescriptor = true });
        _sut = new Provider(_logger, capabilities);
    }

    private static void SetSubmodelData(string jsonContent) => MockData.SubmodelData = JsonDocument.Parse(jsonContent);

    [Fact]
    public void GetManifestData_ValidResource_ReturnsExpectedSemanticIdsAndCapabilities()
    {
        SetSubmodelData(TestData.TestSubmodelData);

        var manifest = _sut.GetManifestData();

        Assert.NotNull(manifest);
        Assert.NotNull(manifest.SupportedSemanticIds);
        Assert.NotEmpty(manifest.SupportedSemanticIds);
        Assert.Contains("Email", manifest.SupportedSemanticIds);
        Assert.Contains("TelephoneNumber", manifest.SupportedSemanticIds);
        Assert.Contains("ClassId", manifest.SupportedSemanticIds);
        Assert.Contains("StatusValue", manifest.SupportedSemanticIds);
        Assert.NotNull(manifest.Capabilities);
        Assert.True(manifest.Capabilities.HasAssetInformation);
        Assert.True(manifest.Capabilities.HasShellDescriptor);
    }

    [Fact]
    public void GetManifestData_EmptyArrayResource_ReturnsNoSupportedSemanticIds()
    {
        SetSubmodelData("{}");

        var manifest = _sut.GetManifestData();

        Assert.NotNull(manifest);
        Assert.NotNull(manifest.SupportedSemanticIds);
        Assert.Empty(manifest.SupportedSemanticIds);
    }

    [Fact]
    public void GetManifestData_WithCapabilitiesFalse_ReturnsCorrectCapabilities()
    {
        const string ValidSubmodelData = @"{
            ""test-submodelId"": {
            ""Email"": ""test@example.com""
            }
            }";

        SetSubmodelData(ValidSubmodelData);
        var capabilities = Substitute.For<IOptions<Capabilities>>();
        capabilities.Value.Returns(new Capabilities { HasAssetInformation = false, HasShellDescriptor = false });
        var sut = new Provider(_logger, capabilities);

        var manifest = sut.GetManifestData();

        Assert.NotNull(manifest);
        Assert.NotNull(manifest.Capabilities);
        Assert.False(manifest.Capabilities.HasAssetInformation);
        Assert.False(manifest.Capabilities.HasShellDescriptor);
    }
}

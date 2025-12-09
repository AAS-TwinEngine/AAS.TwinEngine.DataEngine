using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Config;
using AAS.TwinEngine.DataEngine.DomainModel.Plugin;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Providers.PluginDataProvider.Services;

public class PluginManifestConflictHandlerTests
{
    private readonly ILogger<PluginManifestConflictHandler> _logger = Substitute.For<ILogger<PluginManifestConflictHandler>>();

    private static PluginManifestConflictHandler CreateSut(MultiPluginConflictOptions.MultiPluginConflictOption handlingMode, ILogger<PluginManifestConflictHandler> logger)
    {
        var options = Options.Create(new MultiPluginConflictOptions { HandlingMode = handlingMode });
        return new PluginManifestConflictHandler(logger, options);
    }

    [Fact]
    public async Task InitializeAsync_ThrowsArgumentNull_WhenInputIsNull()
    {
        var sut = CreateSut(MultiPluginConflictOptions.MultiPluginConflictOption.TakeFirst, _logger);

        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.ProcessManifests(null!));
    }

    [Fact]
    public async Task InitializeAsync_FirstWins_KeepsFirst_and_RemovesFromLater()
    {
        var sut = CreateSut(MultiPluginConflictOptions.MultiPluginConflictOption.TakeFirst, _logger);
        var manifest1 = CreateManifest("Plugin1", "semanticId-1");
        var manifest2 = CreateManifest("Plugin2", "semanticId-1", "semanticId-2");
        var manifests = new List<PluginManifest> { manifest1, manifest2 };

        await sut.ProcessManifests(manifests);

        Assert.Contains("semanticId-1", manifest1.SupportedSemanticIds);
        Assert.DoesNotContain("semanticId-1", manifest2.SupportedSemanticIds);
        Assert.Contains("semanticId-2", manifest2.SupportedSemanticIds);
        Assert.Equal(2, sut.Manifests.Count);
        Assert.Same(manifest1, sut.Manifests[0]);
        Assert.Same(manifest2, sut.Manifests[1]);
    }

    [Fact]
    public async Task InitializeAsync_DiscardAndNull_RemovesDuplicateFromAllManifests()
    {
        var sut = CreateSut(MultiPluginConflictOptions.MultiPluginConflictOption.SkipConflictingIds, _logger);
        var manifest1 = CreateManifest("Plugin1", "semanticId-1", "semanticId-3");
        var manifest2 = CreateManifest("Plugin2", "semanticId-1", "semanticId-2");
        var manifest3 = CreateManifest("Plugin3", "semanticId-4", "semanticId-1");
        var manifests = new List<PluginManifest> { manifest1, manifest2, manifest3 };

        await sut.ProcessManifests(manifests);

        Assert.DoesNotContain("semanticId-1", manifest1.SupportedSemanticIds);
        Assert.DoesNotContain("semanticId-1", manifest2.SupportedSemanticIds);
        Assert.DoesNotContain("semanticId-1", manifest3.SupportedSemanticIds);
        Assert.Contains("semanticId-3", manifest1.SupportedSemanticIds);
        Assert.Contains("semanticId-2", manifest2.SupportedSemanticIds);
        Assert.Contains("semanticId-4", manifest3.SupportedSemanticIds);
    }

    [Fact]
    public async Task InitializeAsync_ThrowError_ThrowsOnDuplicate()
    {
        var sut = CreateSut(MultiPluginConflictOptions.MultiPluginConflictOption.ThrowError, _logger);
        var manifest1 = CreateManifest("Plugin1", "semanticId-1");
        var manifest2 = CreateManifest("Plugin2", "semanticId-1");
        var manifests = new List<PluginManifest> { manifest1, manifest2 };

        await Assert.ThrowsAsync<InternalDataProcessingException>(() => sut.ProcessManifests(manifests));
    }

    [Fact]
    public async Task InitializeAsync_NullSupportedSemanticIds_IsHandledGracefully()
    {
        var sut = CreateSut(MultiPluginConflictOptions.MultiPluginConflictOption.TakeFirst, _logger);
        var capabilities = new Capabilities
        {
            HasShellDescriptor = false,
            HasAssetInformation = false
        };
        var manifestWithNull = new PluginManifest
        {
            PluginName = "Plugin1",
            PluginUrl = new Uri("https://example.com/hasnull"),
            SupportedSemanticIds = [],
            Capabilities = capabilities
        };

        var manifestWithSemanticId = CreateManifest("Plugin2", "semanticId-1");
        var manifests = new List<PluginManifest> { manifestWithNull, manifestWithSemanticId };

        await sut.ProcessManifests(manifests);

        Assert.NotNull(manifestWithNull.SupportedSemanticIds);
        Assert.Empty(manifestWithNull.SupportedSemanticIds);
        Assert.Contains("semanticId-1", manifestWithSemanticId.SupportedSemanticIds);
    }

    private static PluginManifest CreateManifest(string name, params string[] semanticIds)
        => new()
        {
            PluginName = name,
            PluginUrl = new Uri("https://example.com/" + name),
            SupportedSemanticIds = semanticIds?.ToList() ?? [],
            Capabilities = new Capabilities()
            {
                HasAssetInformation = false,
                HasShellDescriptor = false
            }
        };

}

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.DomainModel.Plugin;
using AAS.TwinEngine.DataEngine.Infrastructure.Configuration.LegacyV1;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Providers.PluginDataProvider.Services;

public class PluginManifestConflictHandlerTests
{
    private readonly ILogger<PluginManifestConflictHandler> _logger = Substitute.For<ILogger<PluginManifestConflictHandler>>();

    private PluginManifestConflictHandler CreateSut(
        MultiPluginConflictOptions.MultiPluginConflictOption handlingMode = MultiPluginConflictOptions.MultiPluginConflictOption.ThrowError)
    {
        var options = Options.Create(new MultiPluginConflictOptions { HandlingMode = handlingMode });
        return new PluginManifestConflictHandler(options, _logger);
    }

    [Fact]
    public async Task InitializeAsync_ThrowsInvalidDependencyException_WhenInputIsNull()
    {
        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidDependencyException>(() => sut.ProcessManifests(null!));
    }

    [Fact]
    public async Task InitializeAsync_ThrowError_ThrowsOnDuplicate()
    {
        var sut = CreateSut();
        var manifest1 = CreateManifest("Plugin1", "semanticId-1");
        var manifest2 = CreateManifest("Plugin2", "semanticId-1");
        var manifests = new List<PluginManifest> { manifest1, manifest2 };

        await Assert.ThrowsAsync<InternalDataProcessingException>(() => sut.ProcessManifests(manifests));
    }

    [Fact]
    public async Task InitializeAsync_NullSupportedSemanticIds_IsHandledGracefully()
    {
        var sut = CreateSut();
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

    [Fact]
    public async Task InitializeAsync_ProcessesManifestsWithoutConflicts_Successfully()
    {
        var sut = CreateSut();
        var manifest1 = CreateManifest("Plugin1", "semanticId-1");
        var manifest2 = CreateManifest("Plugin2", "semanticId-2");
        var manifests = new List<PluginManifest> { manifest1, manifest2 };

        await sut.ProcessManifests(manifests);

        Assert.Equal(2, sut.Manifests.Count);
        Assert.Contains("semanticId-1", manifest1.SupportedSemanticIds);
        Assert.Contains("semanticId-2", manifest2.SupportedSemanticIds);
    }

    [Fact]
    public async Task InitializeAsync_TakeFirst_KeepsDuplicateInFirstPlugin()
    {
        var sut = CreateSut(MultiPluginConflictOptions.MultiPluginConflictOption.TakeFirst);
        var manifest1 = CreateManifest("Plugin1", "semanticId-1");
        var manifest2 = CreateManifest("Plugin2", "semanticId-1");
        var manifests = new List<PluginManifest> { manifest1, manifest2 };

        await sut.ProcessManifests(manifests);

        Assert.Contains("semanticId-1", manifest1.SupportedSemanticIds);
        Assert.DoesNotContain("semanticId-1", manifest2.SupportedSemanticIds);
    }

    [Fact]
    public async Task InitializeAsync_SkipConflictingIds_RemovesDuplicateFromAll()
    {
        var sut = CreateSut(MultiPluginConflictOptions.MultiPluginConflictOption.SkipConflictingIds);
        var manifest1 = CreateManifest("Plugin1", "semanticId-1");
        var manifest2 = CreateManifest("Plugin2", "semanticId-1");
        var manifests = new List<PluginManifest> { manifest1, manifest2 };

        await sut.ProcessManifests(manifests);

        Assert.DoesNotContain("semanticId-1", manifest1.SupportedSemanticIds);
        Assert.DoesNotContain("semanticId-1", manifest2.SupportedSemanticIds);
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

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Providers;
using AAS.TwinEngine.DataEngine.DomainModel.Plugin;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Services;

using Microsoft.Extensions.Logging;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Providers.PluginDataProvider.Services;

public class PluginManifestInitializerTests
{
    private readonly ILogger<PluginManifestInitializer> _logger;
    private readonly IPluginManifestConflictHandler _pluginManifestConflictHandler;
    private readonly IPluginManifestProvider _pluginManifestProvider;
    private readonly PluginManifestInitializer _sut;

    public PluginManifestInitializerTests()
    {
        _logger = Substitute.For<ILogger<PluginManifestInitializer>>();
        _pluginManifestConflictHandler = Substitute.For<IPluginManifestConflictHandler>();
        _pluginManifestProvider = Substitute.For<IPluginManifestProvider>();

        _sut = new PluginManifestInitializer(_logger, _pluginManifestConflictHandler, _pluginManifestProvider);
    }

    [Fact]
    public async Task InitializeAsync_WithManifests_CallsRegistryInitialize()
    {
        var manifest1 = CreateDomainManifest("PluginA", "semanticId-1");
        var manifest2 = CreateDomainManifest("PluginB", "semanticId-1", "semanticId-2");
        IList<PluginManifest> manifests = new List<PluginManifest> { manifest1, manifest2 };
        _pluginManifestProvider
            .GetAllPluginManifestsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(manifests));

        await _sut.InitializeAsync(CancellationToken.None);

        await _pluginManifestConflictHandler.Received(1).ProcessManifests(Arg.Is<IList<PluginManifest>>(list => list.Count == 2 &&
                                                                                            list[0].PluginName == manifest1.PluginName &&
                                                                                            list[1].PluginName == manifest2.PluginName));
    }

    [Fact]
    public async Task InitializeAsync_WhenProviderThrows_PropagatesException_AndDoesNotCallRegistry()
    {
        var expected = new InvalidOperationException("data provider failed");
        _pluginManifestProvider
            .GetAllPluginManifestsAsync(Arg.Any<CancellationToken>())
            .Returns<Task<IList<PluginManifest>>>(_ => throw expected);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.InitializeAsync(CancellationToken.None));
        Assert.Same(expected, ex);
        await _pluginManifestConflictHandler.DidNotReceiveWithAnyArgs().ProcessManifests(null!);
    }

    [Fact]
    public async Task InitializeAsync_WhenHandlerThrowsInternalDataProcessingException_RethrowsResponseParsingException()
    {
        var manifests = new List<PluginManifest> { CreateDomainManifest("PluginX", "semanticId-1") };
        _pluginManifestProvider
            .GetAllPluginManifestsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IList<PluginManifest>>(manifests));

        _pluginManifestConflictHandler
            .ProcessManifests(Arg.Any<IList<PluginManifest>>())
            .Returns<Task>(_ => throw new InternalDataProcessingException());

        await Assert.ThrowsAsync<MultiPluginConflictException>(() => _sut.InitializeAsync(CancellationToken.None));
    }

    private static PluginManifest CreateDomainManifest(string name, params string[] semanticIds)
        => new()
        {
            PluginName = name,
            PluginUrl = new Uri($"https://example.com/{name}"),
            SupportedSemanticIds = semanticIds?.ToList() ?? [],
            Capabilities = new Capabilities
            {
                HasShellDescriptor = false,
                HasAssetInformation = false
            }
        };
}

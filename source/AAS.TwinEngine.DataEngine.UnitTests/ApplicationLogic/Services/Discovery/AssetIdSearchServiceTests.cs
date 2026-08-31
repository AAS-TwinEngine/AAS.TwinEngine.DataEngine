using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Discovery;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.AasRepository;
using AAS.TwinEngine.DataEngine.DomainModel.Discovery;
using AAS.TwinEngine.DataEngine.DomainModel.Plugin;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;

using AasCore.Aas3_1;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Services.Discovery;

public class AssetIdSearchServiceTests
{
    private readonly IPluginDataHandler _pluginDataHandler = Substitute.For<IPluginDataHandler>();
    private readonly IPluginManifestConflictHandler _pluginManifestConflictHandler = Substitute.For<IPluginManifestConflictHandler>();
    private readonly IAasRepositoryService _aasRepositoryService = Substitute.For<IAasRepositoryService>();
    private readonly AssetIdSearchService _sut;

    public AssetIdSearchServiceTests()
    {
        _sut = new AssetIdSearchService(_pluginDataHandler, _pluginManifestConflictHandler, _aasRepositoryService);
        _ = _pluginManifestConflictHandler.Manifests.Returns(CreatePluginManifests());
    }

    [Fact]
    public async Task SearchShellsByAssetLinkAsync_ReturnsAasIds()
    {
        var assetLinks = new List<AssetLink>
        {
            new() { Name = "SerialNumber", Value = "SN-4711" }
        };

        var metadata = new ShellDescriptorsMetaData
        {
            PagingMetaData = new PagingMetaData { Cursor = null },
            ShellDescriptors =
            [
                new ShellDescriptorMetaData { Id = "urn:example:aas:001", IdShort = "Motor001" },
                new ShellDescriptorMetaData { Id = "urn:example:aas:002", IdShort = "Motor002" }
            ]
        };

        _ = _pluginDataHandler.GetDataForShellsByAssetIdsAsync(
            Arg.Any<IReadOnlyList<PluginManifest>>(),
            Arg.Any<ShellSearchFilter?>(),
            Arg.Any<int>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns(metadata);

        var result = await _sut.SearchShellsByAssetLinkAsync(assetLinks, 100, null, CancellationToken.None);

        Assert.Equal(2, result.Result!.Count);
        Assert.Equal("urn:example:aas:001", result.Result![0]);
        Assert.Equal("urn:example:aas:002", result.Result![1]);
        Assert.Null(result.PagingMetaData?.Cursor);
    }

    [Fact]
    public async Task SearchShellsByAssetLinkAsync_WithPagination_ReturnsPagedResults()
    {
        var assetLinks = new List<AssetLink>
        {
            new() { Name = "SerialNumber", Value = "SN-4711" }
        };

        var allDescriptors = Enumerable.Range(1, 5)
            .Select(i => new ShellDescriptorMetaData { Id = $"urn:example:aas:{i:D3}", IdShort = $"Motor{i}" })
            .ToList();

        var metadata = new ShellDescriptorsMetaData
        {
            PagingMetaData = new PagingMetaData { Cursor = null },
            ShellDescriptors = allDescriptors
        };

        _ = _pluginDataHandler.GetDataForShellsByAssetIdsAsync(
            Arg.Any<IReadOnlyList<PluginManifest>>(),
            Arg.Any<ShellSearchFilter?>(),
            Arg.Any<int>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns(metadata);

        var result = await _sut.SearchShellsByAssetLinkAsync(assetLinks, 2, null, CancellationToken.None);

        Assert.Equal(2, result.Result!.Count);
    }

    [Fact]
    public async Task SearchShellsByAssetLinkAsync_WhenPluginTimeout_ThrowsPluginNotAvailableException()
    {
        var assetLinks = new List<AssetLink>
        {
            new() { Name = "SerialNumber", Value = "SN-4711" }
        };

        _ = _pluginDataHandler.GetDataForShellsByAssetIdsAsync(
            Arg.Any<IReadOnlyList<PluginManifest>>(),
            Arg.Any<ShellSearchFilter?>(),
            Arg.Any<int>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Throws(new RequestTimeoutException());

        await Assert.ThrowsAsync<PluginNotAvailableException>(() => _sut.SearchShellsByAssetLinkAsync(assetLinks, 100, null, CancellationToken.None));
    }

    [Fact]
    public async Task SearchShellsByAssetLinkAsync_WhenUnauthorized_ThrowsServiceUnAuthorizedException()
    {
        var assetLinks = new List<AssetLink>
        {
            new() { Name = "SerialNumber", Value = "SN-4711" }
        };

        _ = _pluginDataHandler.GetDataForShellsByAssetIdsAsync(
            Arg.Any<IReadOnlyList<PluginManifest>>(),
            Arg.Any<ShellSearchFilter?>(),
            Arg.Any<int>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Throws(new AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure.UnauthorizedAccessException());

        await Assert.ThrowsAsync<ServiceUnAuthorizedException>(() => _sut.SearchShellsByAssetLinkAsync(assetLinks, 100, null, CancellationToken.None));
    }

    [Fact]
    public async Task SearchShellsByAssetLinkAsync_WhenResponseParsingError_ThrowsInternalDataProcessingException()
    {
        var assetLinks = new List<AssetLink>
        {
            new() { Name = "SerialNumber", Value = "SN-4711" }
        };

        _ = _pluginDataHandler.GetDataForShellsByAssetIdsAsync(
            Arg.Any<IReadOnlyList<PluginManifest>>(),
            Arg.Any<ShellSearchFilter?>(),
            Arg.Any<int>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Throws(new ResponseParsingException());

        await Assert.ThrowsAsync<InternalDataProcessingException>(
            () => _sut.SearchShellsByAssetLinkAsync(assetLinks, 100, null, CancellationToken.None));
    }

    [Fact]
    public async Task SearchShellsByAssetLinkAsync_WhenMultiPluginConflict_ThrowsInternalDataProcessingException()
    {
        var assetLinks = new List<AssetLink>
        {
            new() { Name = "SerialNumber", Value = "SN-4711" }
        };

        _ = _pluginDataHandler.GetDataForShellsByAssetIdsAsync(
            Arg.Any<IReadOnlyList<PluginManifest>>(),
            Arg.Any<ShellSearchFilter?>(),
            Arg.Any<int>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Throws(new MultiPluginConflictException());

        await Assert.ThrowsAsync<InternalDataProcessingException>(() => _sut.SearchShellsByAssetLinkAsync(assetLinks, 100, null, CancellationToken.None));
    }

    [Fact]
    public async Task SearchShellsByAssetLinkAsync_WhenResourceNotFound_ThrowsInternalDataProcessingException()
    {
        var assetLinks = new List<AssetLink>
        {
            new() { Name = "SerialNumber", Value = "SN-4711" }
        };

        _ = _pluginDataHandler.GetDataForShellsByAssetIdsAsync(
            Arg.Any<IReadOnlyList<PluginManifest>>(),
            Arg.Any<ShellSearchFilter?>(),
            Arg.Any<int>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Throws(new ResourceNotFoundException());

        await Assert.ThrowsAsync<InternalDataProcessingException>(() => _sut.SearchShellsByAssetLinkAsync(assetLinks, 100, null, CancellationToken.None));
    }

    [Fact]
    public async Task SearchShellsByAssetLinkAsync_FiltersOutEmptyIds()
    {
        var assetLinks = new List<AssetLink>
        {
            new() { Name = "SerialNumber", Value = "SN-4711" }
        };

        var metadata = new ShellDescriptorsMetaData
        {
            PagingMetaData = new PagingMetaData { Cursor = null },
            ShellDescriptors =
            [
                new ShellDescriptorMetaData { Id = "urn:example:aas:001", IdShort = "Motor001" },
                new ShellDescriptorMetaData { Id = "", IdShort = "Empty" },
                new ShellDescriptorMetaData { Id = "  ", IdShort = "Whitespace" },
                new ShellDescriptorMetaData { Id = "urn:example:aas:002", IdShort = "Motor002" }
            ]
        };

        _ = _pluginDataHandler.GetDataForShellsByAssetIdsAsync(
            Arg.Any<IReadOnlyList<PluginManifest>>(),
            Arg.Any<ShellSearchFilter?>(),
            Arg.Any<int>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns(metadata);

        var result = await _sut.SearchShellsByAssetLinkAsync(assetLinks, 100, null, CancellationToken.None);

        Assert.Equal(2, result.Result!.Count);
        Assert.Equal("urn:example:aas:001", result.Result![0]);
        Assert.Equal("urn:example:aas:002", result.Result![1]);
    }

    [Fact]
    public async Task GetSpecificAssetIdByAasIdentifierAsync_WithValidInput_ReturnsSpecificAssetIds()
    {
        var aasIdentifier = "urn:example:aas:001";
        var shell = Substitute.For<IAssetAdministrationShell>();
        var assetInfo = Substitute.For<IAssetInformation>();
        var specificAssetId = Substitute.For<ISpecificAssetId>();
        var specificAssetIds = new List<ISpecificAssetId> { specificAssetId };

        _ = assetInfo.SpecificAssetIds.Returns(specificAssetIds);
        _ = shell.AssetInformation.Returns(assetInfo);
        _ = _aasRepositoryService.GetShellByIdAsync(aasIdentifier, Arg.Any<CancellationToken>())
            .Returns(shell);

        var result = await _sut.GetSpecificAssetIdByAasIdentifierAsync(aasIdentifier, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Same(specificAssetId, result[0]);
    }

    [Fact]
    public async Task GetSpecificAssetIdByAasIdentifierAsync_WhenShellNotFound_ThrowsNotFoundException()
    {
        var aasIdentifier = "urn:example:aas:001";
        _ = _aasRepositoryService.GetShellByIdAsync(aasIdentifier, Arg.Any<CancellationToken>())
            .Returns((IAssetAdministrationShell)null!);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.GetSpecificAssetIdByAasIdentifierAsync(aasIdentifier, CancellationToken.None));
    }

    [Fact]
    public async Task GetSpecificAssetIdByAasIdentifierAsync_WhenSpecificAssetIdsEmpty_ThrowsNotFoundException()
    {
        var aasIdentifier = "urn:example:aas:001";
        var shell = Substitute.For<IAssetAdministrationShell>();
        var assetInfo = Substitute.For<IAssetInformation>();

        _ = assetInfo.SpecificAssetIds.Returns(new List<ISpecificAssetId>());
        _ = shell.AssetInformation.Returns(assetInfo);
        _ = _aasRepositoryService.GetShellByIdAsync(aasIdentifier, Arg.Any<CancellationToken>())
            .Returns(shell);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.GetSpecificAssetIdByAasIdentifierAsync(aasIdentifier, CancellationToken.None));
    }

    [Fact]
    public async Task GetSpecificAssetIdByAasIdentifierAsync_WhenSpecificAssetIdsNull_ThrowsNotFoundException()
    {
        var aasIdentifier = "urn:example:aas:001";
        var shell = Substitute.For<IAssetAdministrationShell>();
        var assetInfo = Substitute.For<IAssetInformation>();

        _ = assetInfo.SpecificAssetIds.Returns((List<ISpecificAssetId>)null!);
        _ = shell.AssetInformation.Returns(assetInfo);
        _ = _aasRepositoryService.GetShellByIdAsync(aasIdentifier, Arg.Any<CancellationToken>())
            .Returns(shell);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.GetSpecificAssetIdByAasIdentifierAsync(aasIdentifier, CancellationToken.None));
    }

    private static IReadOnlyList<PluginManifest> CreatePluginManifests()
    {
        return new List<PluginManifest>
        {
            new()
            {
                PluginName = "TestPlugin",
                PluginUrl = new Uri("https://test-plugin.com"),
                SupportedSemanticIds = ["urn:semantic:1"],
                Capabilities = new Capabilities
                {
                    HasShellDescriptor = true,
                    HasAssetInformation = true,
                }
            }
        };
    }
}

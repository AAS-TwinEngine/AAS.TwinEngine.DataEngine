using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Discovery;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.Discovery;
using AAS.TwinEngine.DataEngine.DomainModel.Plugin;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;

using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Services.Discovery;

public class AssetIdSearchServiceTests
{
    private readonly IPluginDataHandler _pluginDataHandler = Substitute.For<IPluginDataHandler>();
    private readonly IPluginManifestConflictHandler _pluginManifestConflictHandler = Substitute.For<IPluginManifestConflictHandler>();
    private readonly AssetIdSearchService _sut;

    public AssetIdSearchServiceTests()
    {
        _sut = new AssetIdSearchService(_pluginDataHandler, _pluginManifestConflictHandler);
        _ = _pluginManifestConflictHandler.Manifests.Returns(CreatePluginManifests());
    }

    [Fact]
    public async Task SearchShellsByAssetLinkAsync_ReturnsAasIds()
    {
        var assetLinks = new List<AssetLink>
        {
            new() { Name = "serialNumber", Value = "SN-4711" }
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

        _ = _pluginDataHandler.GetDataForShellDescriptorsByAssetIdsAsync(
            Arg.Any<IReadOnlyList<PluginManifest>>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(metadata);

        var result = await _sut.SearchShellsByAssetLinkAsync(assetLinks, null, null, CancellationToken.None);

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
            new() { Name = "serialNumber", Value = "SN-4711" }
        };

        var allDescriptors = Enumerable.Range(1, 5)
            .Select(i => new ShellDescriptorMetaData { Id = $"urn:example:aas:{i:D3}", IdShort = $"Motor{i}" })
            .ToList();

        var metadata = new ShellDescriptorsMetaData
        {
            PagingMetaData = new PagingMetaData { Cursor = null },
            ShellDescriptors = allDescriptors
        };

        _ = _pluginDataHandler.GetDataForShellDescriptorsByAssetIdsAsync(
            Arg.Any<IReadOnlyList<PluginManifest>>(),
            Arg.Any<string>(),
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
            new() { Name = "serialNumber", Value = "SN-4711" }
        };

        _ = _pluginDataHandler.GetDataForShellDescriptorsByAssetIdsAsync(
            Arg.Any<IReadOnlyList<PluginManifest>>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Throws(new RequestTimeoutException());

        await Assert.ThrowsAsync<PluginNotAvailableException>(
            () => _sut.SearchShellsByAssetLinkAsync(assetLinks, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetShellMetadataByAssetIdsAsync_ReturnsMetadata()
    {
        var filters = new List<SpecificAssetIdFilter>
        {
            new() { Name = "serialNumber", Value = "SN-4711" }
        };

        var metadata = new ShellDescriptorsMetaData
        {
            PagingMetaData = new PagingMetaData { Cursor = null },
            ShellDescriptors =
            [
                new ShellDescriptorMetaData
                {
                    Id = "urn:example:aas:001",
                    IdShort = "Motor001",
                    GlobalAssetId = "urn:example:asset:001"
                }
            ]
        };

        _ = _pluginDataHandler.GetDataForShellDescriptorsByAssetIdsAsync(
            Arg.Any<IReadOnlyList<PluginManifest>>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(metadata);

        var (result, pagingMetaData) = await _sut.GetShellMetadataByAssetIdsAsync(filters, null, null, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("urn:example:aas:001", result[0].Id);
        Assert.Null(pagingMetaData.Cursor);
    }

    [Fact]
    public async Task GetShellMetadataByAssetIdsAsync_WithNoResults_ReturnsEmptyList()
    {
        var filters = new List<SpecificAssetIdFilter>
        {
            new() { Name = "serialNumber", Value = "non-existent" }
        };

        var metadata = new ShellDescriptorsMetaData
        {
            PagingMetaData = new PagingMetaData { Cursor = null },
            ShellDescriptors = []
        };

        _ = _pluginDataHandler.GetDataForShellDescriptorsByAssetIdsAsync(
            Arg.Any<IReadOnlyList<PluginManifest>>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(metadata);

        var (result, _) = await _sut.GetShellMetadataByAssetIdsAsync(filters, null, null, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchShellsByAssetLinkAsync_WhenUnauthorized_ThrowsServiceUnAuthorizedException()
    {
        var assetLinks = new List<AssetLink>
        {
            new() { Name = "serialNumber", Value = "SN-4711" }
        };

        _ = _pluginDataHandler.GetDataForShellDescriptorsByAssetIdsAsync(
            Arg.Any<IReadOnlyList<PluginManifest>>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Throws(new AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure.UnauthorizedAccessException());

        await Assert.ThrowsAsync<ServiceUnAuthorizedException>(
            () => _sut.SearchShellsByAssetLinkAsync(assetLinks, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task SearchShellsByAssetLinkAsync_WhenResponseParsingError_ThrowsInternalDataProcessingException()
    {
        var assetLinks = new List<AssetLink>
        {
            new() { Name = "serialNumber", Value = "SN-4711" }
        };

        _ = _pluginDataHandler.GetDataForShellDescriptorsByAssetIdsAsync(
            Arg.Any<IReadOnlyList<PluginManifest>>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Throws(new ResponseParsingException());

        await Assert.ThrowsAsync<InternalDataProcessingException>(
            () => _sut.SearchShellsByAssetLinkAsync(assetLinks, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task SearchShellsByAssetLinkAsync_WhenMultiPluginConflict_ThrowsInternalDataProcessingException()
    {
        var assetLinks = new List<AssetLink>
        {
            new() { Name = "serialNumber", Value = "SN-4711" }
        };

        _ = _pluginDataHandler.GetDataForShellDescriptorsByAssetIdsAsync(
            Arg.Any<IReadOnlyList<PluginManifest>>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Throws(new MultiPluginConflictException());

        await Assert.ThrowsAsync<InternalDataProcessingException>(
            () => _sut.SearchShellsByAssetLinkAsync(assetLinks, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task SearchShellsByAssetLinkAsync_WhenResourceNotFound_ThrowsInternalDataProcessingException()
    {
        var assetLinks = new List<AssetLink>
        {
            new() { Name = "serialNumber", Value = "SN-4711" }
        };

        _ = _pluginDataHandler.GetDataForShellDescriptorsByAssetIdsAsync(
            Arg.Any<IReadOnlyList<PluginManifest>>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Throws(new ResourceNotFoundException());

        await Assert.ThrowsAsync<InternalDataProcessingException>(
            () => _sut.SearchShellsByAssetLinkAsync(assetLinks, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetShellMetadataByAssetIdsAsync_WithPagination_ReturnsPagedResults()
    {
        var filters = new List<SpecificAssetIdFilter>
        {
            new() { Name = "serialNumber", Value = "SN-4711" }
        };

        var allDescriptors = Enumerable.Range(1, 5)
            .Select(i => new ShellDescriptorMetaData { Id = $"urn:example:aas:{i:D3}", IdShort = $"Motor{i}" })
            .ToList();

        var metadata = new ShellDescriptorsMetaData
        {
            PagingMetaData = new PagingMetaData { Cursor = null },
            ShellDescriptors = allDescriptors
        };

        _ = _pluginDataHandler.GetDataForShellDescriptorsByAssetIdsAsync(
            Arg.Any<IReadOnlyList<PluginManifest>>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(metadata);

        var (result, pagingMetaData) = await _sut.GetShellMetadataByAssetIdsAsync(filters, 2, null, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.NotNull(pagingMetaData.Cursor);
    }

    [Fact]
    public async Task SearchShellsByAssetLinkAsync_SerializesHeaderCorrectly()
    {
        var assetLinks = new List<AssetLink>
        {
            new() { Name = "serialNumber", Value = "SN-4711" },
            new() { Name = "batchId", Value = "B-001" }
        };

        string? capturedHeader = null;
        _ = _pluginDataHandler.GetDataForShellDescriptorsByAssetIdsAsync(
            Arg.Any<IReadOnlyList<PluginManifest>>(),
            Arg.Do<string>(h => capturedHeader = h),
            Arg.Any<CancellationToken>())
            .Returns(new ShellDescriptorsMetaData
            {
                PagingMetaData = new PagingMetaData { Cursor = null },
                ShellDescriptors = []
            });

        await _sut.SearchShellsByAssetLinkAsync(assetLinks, null, null, CancellationToken.None);

        Assert.NotNull(capturedHeader);
        Assert.Contains("serialNumber", capturedHeader);
        Assert.Contains("SN-4711", capturedHeader);
        Assert.Contains("batchId", capturedHeader);
        Assert.Contains("B-001", capturedHeader);
    }

    [Fact]
    public async Task SearchShellsByAssetLinkAsync_FiltersOutEmptyIds()
    {
        var assetLinks = new List<AssetLink>
        {
            new() { Name = "serialNumber", Value = "SN-4711" }
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

        _ = _pluginDataHandler.GetDataForShellDescriptorsByAssetIdsAsync(
            Arg.Any<IReadOnlyList<PluginManifest>>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(metadata);

        var result3 = await _sut.SearchShellsByAssetLinkAsync(assetLinks, null, null, CancellationToken.None);

        Assert.Equal(2, result3.Result!.Count);
        Assert.Equal("urn:example:aas:001", result3.Result![0]);
        Assert.Equal("urn:example:aas:002", result3.Result![1]);
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

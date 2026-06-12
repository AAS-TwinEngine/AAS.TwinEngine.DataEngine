using AAS.TwinEngine.DataEngine.Api.Discovery.Handler;
using AAS.TwinEngine.DataEngine.Api.Discovery.Requests;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Discovery;
using AAS.TwinEngine.DataEngine.DomainModel.Discovery;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;

using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AAS.TwinEngine.DataEngine.UnitTests.Api.Discovery.Handler;

public class DiscoveryHandlerTests
{
    private readonly IAssetIdSearchService _assetIdSearchService = Substitute.For<IAssetIdSearchService>();
    private readonly ILogger<DiscoveryHandler> _logger = Substitute.For<ILogger<DiscoveryHandler>>();
    private readonly DiscoveryHandler _sut;

    public DiscoveryHandlerTests() => _sut = new DiscoveryHandler(_logger, _assetIdSearchService);

    [Fact]
    public async Task SearchShellsByAssetLinkAsync_WithValidInput_ReturnsAasIds()
    {
        var assetLinks = new[]
        {
            new AssetLinkDto { Name = "SerialNumber", Value = "SN-4711" }
        };
        var expectedIds = new List<string> { "urn:example:aas:001" };
        var pagingMetaData = new PagingMetaData { Cursor = null };

        _ = _assetIdSearchService.SearchShellsByAssetLinkAsync(
            Arg.Any<IList<AssetLink>>(), null, null, Arg.Any<CancellationToken>())
            .Returns(new ShellsByAssetLink { PagingMetaData = pagingMetaData, Result = expectedIds });

        var result = await _sut.SearchShellsByAssetLinkAsync(assetLinks, null, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result.Result!);
        Assert.Equal("urn:example:aas:001", result.Result![0]);
        Assert.Null(result.PagingMetaData?.Cursor);
    }

    [Fact]
    public async Task SearchShellsByAssetLinkAsync_WithEmptyArray_ThrowsInvalidUserInputException()
    {
        var assetLinks = Array.Empty<AssetLinkDto>();

        await Assert.ThrowsAsync<InvalidUserInputException>(
            () => _sut.SearchShellsByAssetLinkAsync(assetLinks, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task SearchShellsByAssetLinkAsync_WithNullName_ThrowsInvalidUserInputException()
    {
        var assetLinks = new[]
        {
            new AssetLinkDto { Name = "", Value = "SN-4711" }
        };

        await Assert.ThrowsAsync<InvalidUserInputException>(
            () => _sut.SearchShellsByAssetLinkAsync(assetLinks, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task SearchShellsByAssetLinkAsync_WithNullValue_ThrowsInvalidUserInputException()
    {
        var assetLinks = new[]
        {
            new AssetLinkDto { Name = "SerialNumber", Value = "" }
        };

        await Assert.ThrowsAsync<InvalidUserInputException>(
            () => _sut.SearchShellsByAssetLinkAsync(assetLinks, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task SearchShellsByAssetLinkAsync_WithNameTooLong_ThrowsInvalidUserInputException()
    {
        var assetLinks = new[]
        {
            new AssetLinkDto { Name = new string('a', 65), Value = "SN-4711" }
        };

        await Assert.ThrowsAsync<InvalidUserInputException>(
            () => _sut.SearchShellsByAssetLinkAsync(assetLinks, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task SearchShellsByAssetLinkAsync_WithValueTooLong_ThrowsInvalidUserInputException()
    {
        var assetLinks = new[]
        {
            new AssetLinkDto { Name = "SerialNumber", Value = new string('x', 2049) }
        };

        await Assert.ThrowsAsync<InvalidUserInputException>(
            () => _sut.SearchShellsByAssetLinkAsync(assetLinks, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task SearchShellsByAssetLinkAsync_WithNegativeLimit_ThrowsInvalidUserInputException()
    {
        var assetLinks = new[]
        {
            new AssetLinkDto { Name = "SerialNumber", Value = "SN-4711" }
        };

        await Assert.ThrowsAsync<InvalidUserInputException>(
            () => _sut.SearchShellsByAssetLinkAsync(assetLinks, -1, null, CancellationToken.None));
    }

    [Fact]
    public async Task SearchShellsByAssetLinkAsync_WithMaxLengthName_DoesNotThrow()
    {
        var assetLinks = new[]
        {
            new AssetLinkDto { Name = new string('a', 64), Value = "SN-4711" }
        };
        var expectedIds = new List<string> { "urn:example:aas:001" };
        var pagingMetaData = new PagingMetaData { Cursor = null };

        _ = _assetIdSearchService.SearchShellsByAssetLinkAsync(
            Arg.Any<IList<AssetLink>>(), null, null, Arg.Any<CancellationToken>())
            .Returns(new ShellsByAssetLink { PagingMetaData = pagingMetaData, Result = expectedIds });

        var result = await _sut.SearchShellsByAssetLinkAsync(assetLinks, null, null, CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task SearchShellsByAssetLinkAsync_WithMultipleAssetLinks_PassesAll()
    {
        var assetLinks = new[]
        {
            new AssetLinkDto { Name = "SerialNumber", Value = "SN-4711" },
            new AssetLinkDto { Name = "BatchId", Value = "B-001" }
        };
        var expectedIds = new List<string> { "urn:example:aas:001" };
        var pagingMetaData = new PagingMetaData { Cursor = null };

        _ = _assetIdSearchService.SearchShellsByAssetLinkAsync(
            Arg.Any<IList<AssetLink>>(), null, null, Arg.Any<CancellationToken>())
            .Returns(new ShellsByAssetLink { PagingMetaData = pagingMetaData, Result = expectedIds });

        var result = await _sut.SearchShellsByAssetLinkAsync(assetLinks, null, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result.Result!);
    }

    [Fact]
    public async Task SearchShellsByAssetLinkAsync_WithPagination_ReturnsCursor()
    {
        var assetLinks = new[]
        {
            new AssetLinkDto { Name = "SerialNumber", Value = "SN-4711" }
        };
        var expectedIds = new List<string> { "urn:example:aas:001", "urn:example:aas:002" };
        var pagingMetaData = new PagingMetaData { Cursor = "nextCursorValue" };

        _ = _assetIdSearchService.SearchShellsByAssetLinkAsync(
            Arg.Any<IList<AssetLink>>(), 1, null, Arg.Any<CancellationToken>())
            .Returns(new ShellsByAssetLink { PagingMetaData = pagingMetaData, Result = expectedIds });

        var result = await _sut.SearchShellsByAssetLinkAsync(assetLinks, 1, null, CancellationToken.None);

        Assert.NotNull(result.PagingMetaData);
        Assert.Equal("nextCursorValue", result.PagingMetaData!.Cursor);
    }
}
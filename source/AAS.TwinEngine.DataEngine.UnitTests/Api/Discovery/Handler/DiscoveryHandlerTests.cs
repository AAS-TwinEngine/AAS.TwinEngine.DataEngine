using AAS.TwinEngine.DataEngine.Api.Discovery.Handler;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Discovery;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.Discovery;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;

using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AAS.TwinEngine.DataEngine.UnitTests.Api.Discovery.Handler;

public class DiscoveryHandlerTests
{
    private readonly IAssetIdSearchService _assetIdSearchService = Substitute.For<IAssetIdSearchService>();
    private readonly IAasRepositoryTemplateService _templateService = Substitute.For<IAasRepositoryTemplateService>();
    private readonly ILogger<DiscoveryHandler> _logger = Substitute.For<ILogger<DiscoveryHandler>>();
    private readonly DiscoveryHandler _sut;

    public DiscoveryHandlerTests() => _sut = new DiscoveryHandler(_logger, _assetIdSearchService, _templateService);

    [Fact]
    public async Task SearchShellsByAssetLinkAsync_WithValidInput_ReturnsAasIds()
    {
        var assetLinks = new[]
        {
            new AssetLink { Name = "serialNumber", Value = "SN-4711" }
        };
        var expectedIds = new List<string> { "urn:example:aas:001" };
        var pagingMetaData = new PagingMetaData { Cursor = null };

        _ = _assetIdSearchService.SearchShellsByAssetLinkAsync(
            Arg.Any<IList<AssetLink>>(), null, null, Arg.Any<CancellationToken>())
            .Returns((expectedIds, pagingMetaData));

        var result = await _sut.SearchShellsByAssetLinkAsync(assetLinks, null, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result.Result!);
        Assert.Equal("urn:example:aas:001", result.Result![0]);
        Assert.Null(result.PagingMetaData?.Cursor);
    }

    [Fact]
    public async Task SearchShellsByAssetLinkAsync_WithEmptyArray_ThrowsInvalidUserInputException()
    {
        var assetLinks = Array.Empty<AssetLink>();

        await Assert.ThrowsAsync<InvalidUserInputException>(
            () => _sut.SearchShellsByAssetLinkAsync(assetLinks, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task SearchShellsByAssetLinkAsync_WithNullName_ThrowsInvalidUserInputException()
    {
        var assetLinks = new[]
        {
            new AssetLink { Name = "", Value = "SN-4711" }
        };

        await Assert.ThrowsAsync<InvalidUserInputException>(
            () => _sut.SearchShellsByAssetLinkAsync(assetLinks, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task SearchShellsByAssetLinkAsync_WithNullValue_ThrowsInvalidUserInputException()
    {
        var assetLinks = new[]
        {
            new AssetLink { Name = "serialNumber", Value = "" }
        };

        await Assert.ThrowsAsync<InvalidUserInputException>(
            () => _sut.SearchShellsByAssetLinkAsync(assetLinks, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task SearchShellsByAssetLinkAsync_WithNameTooLong_ThrowsInvalidUserInputException()
    {
        var assetLinks = new[]
        {
            new AssetLink { Name = new string('a', 65), Value = "SN-4711" }
        };

        await Assert.ThrowsAsync<InvalidUserInputException>(
            () => _sut.SearchShellsByAssetLinkAsync(assetLinks, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task SearchShellsByAssetLinkAsync_WithNegativeLimit_ThrowsInvalidUserInputException()
    {
        var assetLinks = new[]
        {
            new AssetLink { Name = "serialNumber", Value = "SN-4711" }
        };

        await Assert.ThrowsAsync<InvalidUserInputException>(
            () => _sut.SearchShellsByAssetLinkAsync(assetLinks, -1, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetShellsByAssetIdsAsync_WithNullAssetIds_ThrowsInvalidUserInputException()
    {
        await Assert.ThrowsAsync<InvalidUserInputException>(
            () => _sut.GetShellsByAssetIdsAsync(null, null, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetShellsByAssetIdsAsync_WithEmptyAssetIds_ThrowsInvalidUserInputException()
    {
        await Assert.ThrowsAsync<InvalidUserInputException>(
            () => _sut.GetShellsByAssetIdsAsync([], null, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetShellsByAssetIdsAsync_WithInvalidBase64Url_ThrowsInvalidUserInputException()
    {
        var assetIds = new[] { "not-valid-base64!!!" };

        await Assert.ThrowsAsync<InvalidUserInputException>(
            () => _sut.GetShellsByAssetIdsAsync(assetIds, null, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetShellsByAssetIdsAsync_WithInvalidJson_ThrowsInvalidUserInputException()
    {
        var invalidJson = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("not json"))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var assetIds = new[] { invalidJson };

        await Assert.ThrowsAsync<InvalidUserInputException>(
            () => _sut.GetShellsByAssetIdsAsync(assetIds, null, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetShellsByAssetIdsAsync_WithMissingName_ThrowsInvalidUserInputException()
    {
        var json = """{"value":"SN-4711"}""";
        var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var assetIds = new[] { encoded };

        await Assert.ThrowsAsync<InvalidUserInputException>(
            () => _sut.GetShellsByAssetIdsAsync(assetIds, null, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetShellsByAssetIdsAsync_WithMissingValue_ThrowsInvalidUserInputException()
    {
        var json = """{"name":"serialNumber"}""";
        var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var assetIds = new[] { encoded };

        await Assert.ThrowsAsync<InvalidUserInputException>(
            () => _sut.GetShellsByAssetIdsAsync(assetIds, null, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetShellsByAssetIdsAsync_WithValidInput_ReturnsShells()
    {
        var json = """{"name":"serialNumber","value":"SN-4711"}""";
        var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var assetIds = new[] { encoded };

        var metadata = new List<ShellDescriptorMetaData>
        {
            new() { Id = "urn:example:aas:001", IdShort = "Motor001", GlobalAssetId = "urn:example:asset:001" }
        };
        var pagingMetaData = new PagingMetaData { Cursor = null };

        _ = _assetIdSearchService.GetShellMetadataByAssetIdsAsync(
            Arg.Any<IList<SpecificAssetIdFilter>>(), null, null, Arg.Any<CancellationToken>())
            .Returns((metadata, pagingMetaData));

        var shell = new AasCore.Aas3_0.AssetAdministrationShell(
            "urn:example:aas:001",
            new AasCore.Aas3_0.AssetInformation(AasCore.Aas3_0.AssetKind.Instance));

        _ = _templateService.GetShellTemplateAsync("urn:example:aas:001", Arg.Any<CancellationToken>())
            .Returns(shell);

        var result = await _sut.GetShellsByAssetIdsAsync(assetIds, null, null, null, CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task SearchShellsByAssetLinkAsync_WithValueTooLong_ThrowsInvalidUserInputException()
    {
        var assetLinks = new[]
        {
            new AssetLink { Name = "serialNumber", Value = new string('x', 2049) }
        };

        await Assert.ThrowsAsync<InvalidUserInputException>(
            () => _sut.SearchShellsByAssetLinkAsync(assetLinks, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task SearchShellsByAssetLinkAsync_WithMaxLengthName_DoesNotThrow()
    {
        var assetLinks = new[]
        {
            new AssetLink { Name = new string('a', 64), Value = "SN-4711" }
        };
        var expectedIds = new List<string> { "urn:example:aas:001" };
        var pagingMetaData = new PagingMetaData { Cursor = null };

        _ = _assetIdSearchService.SearchShellsByAssetLinkAsync(
            Arg.Any<IList<AssetLink>>(), null, null, Arg.Any<CancellationToken>())
            .Returns((expectedIds, pagingMetaData));

        var result = await _sut.SearchShellsByAssetLinkAsync(assetLinks, null, null, CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task SearchShellsByAssetLinkAsync_WithMultipleAssetLinks_PassesAll()
    {
        var assetLinks = new[]
        {
            new AssetLink { Name = "serialNumber", Value = "SN-4711" },
            new AssetLink { Name = "batchId", Value = "B-001" }
        };
        var expectedIds = new List<string> { "urn:example:aas:001" };
        var pagingMetaData = new PagingMetaData { Cursor = null };

        _ = _assetIdSearchService.SearchShellsByAssetLinkAsync(
            Arg.Any<IList<AssetLink>>(), null, null, Arg.Any<CancellationToken>())
            .Returns((expectedIds, pagingMetaData));

        var result = await _sut.SearchShellsByAssetLinkAsync(assetLinks, null, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result.Result!);
    }

    [Fact]
    public async Task GetShellsByAssetIdsAsync_WithNameTooLong_ThrowsInvalidUserInputException()
    {
        var json = $$"""{"name":"{{new string('a', 65)}}","value":"SN-4711"}""";
        var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var assetIds = new[] { encoded };

        await Assert.ThrowsAsync<InvalidUserInputException>(
            () => _sut.GetShellsByAssetIdsAsync(assetIds, null, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetShellsByAssetIdsAsync_WithValueTooLong_ThrowsInvalidUserInputException()
    {
        var json = $$"""{"name":"serialNumber","value":"{{new string('x', 2049)}}"}""";
        var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var assetIds = new[] { encoded };

        await Assert.ThrowsAsync<InvalidUserInputException>(
            () => _sut.GetShellsByAssetIdsAsync(assetIds, null, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetShellsByAssetIdsAsync_WithEmptyEncodedValue_ThrowsInvalidUserInputException()
    {
        var assetIds = new[] { "" };

        await Assert.ThrowsAsync<InvalidUserInputException>(
            () => _sut.GetShellsByAssetIdsAsync(assetIds, null, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetShellsByAssetIdsAsync_WithMultipleValidAssetIds_ReturnsResults()
    {
        var json1 = """{"name":"serialNumber","value":"SN-4711"}""";
        var json2 = """{"name":"batchId","value":"B-001"}""";
        var encoded1 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json1))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var encoded2 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json2))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var assetIds = new[] { encoded1, encoded2 };

        var metadata = new List<ShellDescriptorMetaData>
        {
            new() { Id = "urn:example:aas:001", IdShort = "Motor001", GlobalAssetId = "urn:example:asset:001" }
        };
        var pagingMetaData = new PagingMetaData { Cursor = null };

        _ = _assetIdSearchService.GetShellMetadataByAssetIdsAsync(
            Arg.Any<IList<SpecificAssetIdFilter>>(), null, null, Arg.Any<CancellationToken>())
            .Returns((metadata, pagingMetaData));

        var shell = new AasCore.Aas3_0.AssetAdministrationShell(
            "urn:example:aas:001",
            new AasCore.Aas3_0.AssetInformation(AasCore.Aas3_0.AssetKind.Instance));

        _ = _templateService.GetShellTemplateAsync("urn:example:aas:001", Arg.Any<CancellationToken>())
            .Returns(shell);

        var result = await _sut.GetShellsByAssetIdsAsync(assetIds, null, null, null, CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetShellsByAssetIdsAsync_WhenTemplateThrowsInternalDataProcessing_SkipsShell()
    {
        var json = """{"name":"serialNumber","value":"SN-4711"}""";
        var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var assetIds = new[] { encoded };

        var metadata = new List<ShellDescriptorMetaData>
        {
            new() { Id = "urn:example:aas:001", IdShort = "Motor001" },
            new() { Id = "urn:example:aas:002", IdShort = "Motor002" }
        };
        var pagingMetaData = new PagingMetaData { Cursor = null };

        _ = _assetIdSearchService.GetShellMetadataByAssetIdsAsync(
            Arg.Any<IList<SpecificAssetIdFilter>>(), null, null, Arg.Any<CancellationToken>())
            .Returns((metadata, pagingMetaData));

        _ = _templateService.GetShellTemplateAsync("urn:example:aas:001", Arg.Any<CancellationToken>())
            .Returns<AasCore.Aas3_0.IAssetAdministrationShell>(x => throw new InternalDataProcessingException());

        var shell2 = new AasCore.Aas3_0.AssetAdministrationShell(
            "urn:example:aas:002",
            new AasCore.Aas3_0.AssetInformation(AasCore.Aas3_0.AssetKind.Instance));
        _ = _templateService.GetShellTemplateAsync("urn:example:aas:002", Arg.Any<CancellationToken>())
            .Returns(shell2);

        var result = await _sut.GetShellsByAssetIdsAsync(assetIds, null, null, null, CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetShellsByAssetIdsAsync_WithNegativeLimit_ThrowsInvalidUserInputException()
    {
        var json = """{"name":"serialNumber","value":"SN-4711"}""";
        var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var assetIds = new[] { encoded };

        await Assert.ThrowsAsync<InvalidUserInputException>(
            () => _sut.GetShellsByAssetIdsAsync(assetIds, null, -1, null, CancellationToken.None));
    }

    [Fact]
    public async Task SearchShellsByAssetLinkAsync_WithPagination_ReturnsCursor()
    {
        var assetLinks = new[]
        {
            new AssetLink { Name = "serialNumber", Value = "SN-4711" }
        };
        var expectedIds = new List<string> { "urn:example:aas:001", "urn:example:aas:002" };
        var pagingMetaData = new PagingMetaData { Cursor = "nextCursorValue" };

        _ = _assetIdSearchService.SearchShellsByAssetLinkAsync(
            Arg.Any<IList<AssetLink>>(), 1, null, Arg.Any<CancellationToken>())
            .Returns((expectedIds, pagingMetaData));

        var result = await _sut.SearchShellsByAssetLinkAsync(assetLinks, 1, null, CancellationToken.None);

        Assert.NotNull(result.PagingMetaData);
        Assert.Equal("nextCursorValue", result.PagingMetaData!.Cursor);
    }
}

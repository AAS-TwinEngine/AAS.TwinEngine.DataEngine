using AAS.TwinEngine.DataEngine.Api.Discovery;
using AAS.TwinEngine.DataEngine.Api.Discovery.Handler;
using AAS.TwinEngine.DataEngine.Api.Discovery.Requests;
using AAS.TwinEngine.DataEngine.Api.Discovery.Responses;
using AAS.TwinEngine.DataEngine.Api.Shared;

using AasCore.Aas3_1;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.Api.Discovery;

public class DiscoveryControllerTests
{
    private readonly IDiscoveryHandler _handler;
    private readonly DiscoveryController _sut;

    public DiscoveryControllerTests()
    {
        var logger = Substitute.For<ILogger<DiscoveryController>>();
        _handler = Substitute.For<IDiscoveryHandler>();
        _sut = new DiscoveryController(logger, _handler);
    }

    [Fact]
    public async Task SearchShellsByAssetLinkAsync_ReturnsOkResult()
    {
        var assetLinks = new[]
        {
            new AssetLinkDto { Name = "SerialNumber", Value = "SN-4711" }
        };
        var expectedResponse = new ShellsByAssetLinkResponseDto
        {
            PagingMetaData = new PagingMetaDataDto { Cursor = null },
            Result = ["urn:example:aas:motor:001"]
        };
        var request = new SearchShellsByAssetLinkRequest(assetLinks, null, null);
        _ = _handler.SearchShellsByAssetLinkAsync(request, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        var result = await _sut.SearchShellsByAssetLinkAsync(assetLinks, null, null, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ShellsByAssetLinkResponseDto>(okResult.Value);
        Assert.Single(response.Result!);
        Assert.Equal("urn:example:aas:motor:001", response.Result![0]);
        Assert.Null(response.PagingMetaData?.Cursor);
    }

    [Fact]
    public async Task SearchShellsByAssetLinkAsync_WithPagination_PassesParameters()
    {
        var assetLinks = new[]
        {
            new AssetLinkDto { Name = "SerialNumber", Value = "SN-4711" }
        };
        var expectedResponse = new ShellsByAssetLinkResponseDto
        {
            PagingMetaData = new PagingMetaDataDto { Cursor = "nextCursor" },
            Result = ["urn:example:aas:motor:001"]
        };
        var request = new SearchShellsByAssetLinkRequest(assetLinks, 10, "cursor123");
        _ = _handler.SearchShellsByAssetLinkAsync(request, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        var result = await _sut.SearchShellsByAssetLinkAsync(assetLinks, 10, "cursor123", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetSpecificAssetIdByAasIdentifierAsync_ReturnsOkResult()
    {
        var aasIdentifier = "dXJuOmV4YW1wbGU6YWFzOjAwMQ"; // Base64Url-encoded "urn:example:aas:001"
        var expectedSpecificAssetIds = new List<ISpecificAssetId> { Substitute.For<ISpecificAssetId>() };
        var request = new GetSpecificAssetIdByAasIdentifierRequest(aasIdentifier);

        _ = _handler.GetSpecificAssetIdByAasIdentifierAsync(request, Arg.Any<CancellationToken>())
            .Returns(expectedSpecificAssetIds);

        var result = await _sut.GetSpecificAssetIdByAasIdentifierAsync(aasIdentifier, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<List<ISpecificAssetId>>(okResult.Value);
        Assert.Single(response);
        Assert.Same(expectedSpecificAssetIds[0], response[0]);
    }
}

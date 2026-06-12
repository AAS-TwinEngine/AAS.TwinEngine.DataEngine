using AAS.TwinEngine.DataEngine.Api.Discovery;
using AAS.TwinEngine.DataEngine.Api.Discovery.Handler;
using AAS.TwinEngine.DataEngine.Api.Discovery.Requests;
using AAS.TwinEngine.DataEngine.Api.Discovery.Responses;
using AAS.TwinEngine.DataEngine.Api.Shared;
using AAS.TwinEngine.DataEngine.DomainModel.Discovery;

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
        _ = _handler.SearchShellsByAssetLinkAsync(assetLinks, null, null, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        var result = await _sut.SearchShellsByAssetLinkAsync(assetLinks, null, null, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ShellsByAssetLinkResponseDto>(okResult.Value);
        Assert.Single(response.Result!);
        Assert.Equal("urn:example:aas:motor:001", response.Result![0]);
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
        _ = _handler.SearchShellsByAssetLinkAsync(assetLinks, 10, "cursor123", Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        var result = await _sut.SearchShellsByAssetLinkAsync(assetLinks, 10, "cursor123", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }
}

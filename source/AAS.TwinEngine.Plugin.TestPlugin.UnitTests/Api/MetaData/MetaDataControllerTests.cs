using Aas.TwinEngine.Plugin.TestPlugin.Api.MetaData;
using Aas.TwinEngine.Plugin.TestPlugin.Api.MetaData.Handler;
using Aas.TwinEngine.Plugin.TestPlugin.Api.MetaData.Requests;
using Aas.TwinEngine.Plugin.TestPlugin.Api.MetaData.Responses;
using Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Exceptions;

using Microsoft.AspNetCore.Mvc;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Aas.TwinEngine.Plugin.TestPlugin.UnitTests.Api.MetaData;

public class MetaDataControllerTests
{
    private readonly IMetaDataHandler _handler = Substitute.For<IMetaDataHandler>();
    private readonly MetaDataController _sut;
    private const string AasIdentifier = "dGVzdA==";

    public MetaDataControllerTests() => _sut = new MetaDataController(_handler);

    [Fact]
    public async Task GetShellDescriptorsAsync_ReturnsOk_WithShellList()
    {
        var request = new GetShellDescriptorsRequest(null, null);
        var expectedShells = new ShellDescriptorsDto();
        _handler.GetShellDescriptors(request, Arg.Any<CancellationToken>()).Returns(expectedShells);

        var result = await _sut.GetShellDescriptorsAsync(null, null, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var actualShells = Assert.IsType<ShellDescriptorsDto>(okResult.Value);
        Assert.Equal(expectedShells, actualShells);
    }

    [Fact]
    public async Task GetShellDescriptorsAsync_ThrowsNotFoundException_Returns404()
    {
        var request = new GetShellDescriptorsRequest(null, null);
        _handler.GetShellDescriptors(request, Arg.Any<CancellationToken>())
        .Throws(new NotFoundException("Shell not found"));

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetShellDescriptorsAsync(null, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetShellDescriptorsAsync_ThrowsBadRequestException_Returns400()
    {
        var request = new GetShellDescriptorsRequest(null, null);
        _handler.GetShellDescriptors(request, Arg.Any<CancellationToken>())
                .Throws(new BadRequestException("Invalid request"));

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.GetShellDescriptorsAsync(null, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetShellDescriptorsAsync_ThrowsException_Returns500()
    {
        var request = new GetShellDescriptorsRequest(null, null);
        _handler.GetShellDescriptors(request, Arg.Any<CancellationToken>())
                .Throws(new Exception("Unexpected error"));

        await Assert.ThrowsAsync<Exception>(() => _sut.GetShellDescriptorsAsync(null, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetShellDescriptorAsync_ReturnsOk_WithShell()
    {
        var expectedShell = new ShellDescriptorDto();
        _handler.GetShellDescriptor(Arg.Any<GetShellDescriptorRequest>(), Arg.Any<CancellationToken>())
        .Returns(expectedShell);

        var result = await _sut.GetShellDescriptorAsync(AasIdentifier, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var actualShell = Assert.IsType<ShellDescriptorDto>(okResult.Value);
        Assert.Equal(expectedShell, actualShell);
    }

    [Fact]
    public async Task GetShellDescriptorAsync_ThrowsNotFoundException_Returns404()
    {
        _handler.GetShellDescriptor(Arg.Any<GetShellDescriptorRequest>(), Arg.Any<CancellationToken>())
        .Throws(new NotFoundException("Shell not found"));

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetShellDescriptorAsync(AasIdentifier, CancellationToken.None));
    }

    [Fact]
    public async Task GetShellDescriptorAsync_ThrowsBadRequestException_Returns400()
    {
        _handler.GetShellDescriptor(Arg.Any<GetShellDescriptorRequest>(), Arg.Any<CancellationToken>())
                .Throws(new BadRequestException("Invalid AAS identifier"));

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.GetShellDescriptorAsync(AasIdentifier, CancellationToken.None));
    }

    [Fact]
    public async Task GetShellDescriptorAsync_ThrowsException_Returns500()
    {
        _handler.GetShellDescriptor(Arg.Any<GetShellDescriptorRequest>(), Arg.Any<CancellationToken>())
                .Throws(new Exception("Unexpected error"));

        await Assert.ThrowsAsync<Exception>(() => _sut.GetShellDescriptorAsync(AasIdentifier, CancellationToken.None));
    }

    [Fact]
    public async Task GetAssetAsync_ReturnsOk_WithAssetInformation()
    {
        var expectedAssetInfo = new AssetDto();
        _handler.GetAsset(Arg.Any<GetAssetRequest>(), Arg.Any<CancellationToken>())
                .Returns(expectedAssetInfo);

        var result = await _sut.GetAssetAsync(AasIdentifier, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var actualAssetInfo = Assert.IsType<AssetDto>(okResult.Value);
        Assert.Equal(expectedAssetInfo, actualAssetInfo);
    }

    [Fact]
    public async Task GetAssetAsync_ThrowsNotFoundException_Returns404()
    {
        _handler.GetAsset(Arg.Any<GetAssetRequest>(), Arg.Any<CancellationToken>())
                .Throws(new NotFoundException("Asset not found"));

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetAssetAsync(AasIdentifier, CancellationToken.None));
    }

    [Fact]
    public async Task GetAssetAsync_ThrowsBadRequestException_Returns400()
    {
        _handler.GetAsset(Arg.Any<GetAssetRequest>(), Arg.Any<CancellationToken>())
                .Throws(new BadRequestException("Invalid request"));

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.GetAssetAsync(AasIdentifier, CancellationToken.None));
    }

    [Fact]
    public async Task GetAssetAsync_ThrowsException_Returns500()
    {
        _handler.GetAsset(Arg.Any<GetAssetRequest>(), Arg.Any<CancellationToken>())
                .Throws(new Exception("Unexpected error"));

        await Assert.ThrowsAsync<Exception>(() =>
                                                _sut.GetAssetAsync(AasIdentifier, CancellationToken.None));
    }
}

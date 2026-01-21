using AAS.TwinEngine.Plugin.TestPlugin.Api.MetaData.Handler;
using AAS.TwinEngine.Plugin.TestPlugin.Api.MetaData.Requests;
using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Constants;
using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Exceptions;
using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.MetaData;
using AAS.TwinEngine.Plugin.TestPlugin.DomainModel.MetaData;

using Microsoft.Extensions.Logging;

using NSubstitute;

namespace AAS.TwinEngine.Plugin.TestPlugin.UnitTests.Api.MetaData.Handler;

public class MetaDataHandlerTests
{
    private readonly ILogger<MetaDataHandler> _logger = Substitute.For<ILogger<MetaDataHandler>>();
    private readonly IMetaDataService _shellDescriptorService = Substitute.For<IMetaDataService>();
    private readonly MetaDataHandler _sut;

    public MetaDataHandlerTests() => _sut = new MetaDataHandler(_logger, _shellDescriptorService);

    [Fact]
    public async Task GetShellDescriptors_ReturnsShellDescriptorsDto_WhenDescriptorsExist()
    {
        var request = new GetShellDescriptorsRequest(10, "cursor123");
        var shellDescriptorsData = new ShellDescriptorsData
        {
            PagingMetaData = new PagingMetaData() { Cursor = "nextCursor" },
            Result = new List<ShellDescriptorData>()
            {
                new() { Id = "desc1" },
                new() { Id = "desc2" }
            }
        };
        _shellDescriptorService.GetShellDescriptorsAsync(request.Limit, request.Cursor, Arg.Any<CancellationToken>())
                               .Returns(shellDescriptorsData);

        var result = await _sut.GetShellDescriptors(request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result.Result!.Count);
        Assert.Equal("desc1", result.Result![0].Id);
        Assert.Equal("desc2", result.Result![1].Id);
        Assert.Equal("nextCursor", result.PagingMetaData!.Cursor);
    }

    [Fact]
    public async Task GetShellDescriptors_ThrowBadRequest_WhenLimitIsZero()
    {
        var request = new GetShellDescriptorsRequest(0, "cursor123");

        var record = await Assert.ThrowsAsync<BadRequestException>(() =>
                                                        _sut.GetShellDescriptors(request, CancellationToken.None));
        Assert.Equal(ExceptionMessages.InvalidRequestedLimit, record.Message);
    }

    [Fact]
    public async Task GetShellDescriptor_ReturnsShellDescriptor_WhenExists()
    {
        var request = new GetShellDescriptorRequest("test");
        var shellMetaData = new ShellDescriptorData { Id = "test" };
        _shellDescriptorService.GetShellDescriptorAsync("test", Arg.Any<CancellationToken>()).Returns(shellMetaData);

        var result = await _sut.GetShellDescriptor(request, CancellationToken.None);

        Assert.Equal(result.Id, shellMetaData.Id);
    }

    [Fact]
    public async Task GetShellDescriptor_ThrowsNotFoundException_WhenShellDoesNotExist()
    {
        var request = new GetShellDescriptorRequest("test");
        _shellDescriptorService.GetShellDescriptorAsync("test", Arg.Any<CancellationToken>())!.Returns((ShellDescriptorData)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetShellDescriptor(request, CancellationToken.None));
    }

    [Fact]
    public async Task GetAsset_ReturnsAsset_WhenExists()
    {
        var request = new GetAssetRequest("test-shell");
        var assetMetaData = new AssetData { GlobalAssetId = "test-shell" };
        _shellDescriptorService.GetAssetAsync("test-shell", Arg.Any<CancellationToken>())
                               .Returns(assetMetaData);

        var result = await _sut.GetAsset(request, CancellationToken.None);

        Assert.Equal(assetMetaData.GlobalAssetId, result.GlobalAssetId);
    }

    [Fact]
    public async Task GetAsset_ThrowsNotFoundException_WhenAssetDoesNotExist()
    {
        var request = new GetAssetRequest("test-shell");
        _shellDescriptorService.GetAssetAsync("test-shell", Arg.Any<CancellationToken>())
                               .Returns((AssetData)null!);

        await Assert.ThrowsAsync<NotFoundException>(() =>
                                                        _sut.GetAsset(request, CancellationToken.None));
    }
}

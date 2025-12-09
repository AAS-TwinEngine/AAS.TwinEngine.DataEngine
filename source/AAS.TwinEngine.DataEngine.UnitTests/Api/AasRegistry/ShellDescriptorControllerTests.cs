using AAS.TwinEngine.DataEngine.Api.AasRegistry;
using AAS.TwinEngine.DataEngine.Api.AasRegistry.Handler;
using AAS.TwinEngine.DataEngine.Api.AasRegistry.Requests;
using AAS.TwinEngine.DataEngine.Api.AasRegistry.Responses;
using AAS.TwinEngine.DataEngine.Api.Shared;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;

using AasCore.Aas3_0;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using UnauthorizedAccessException = System.UnauthorizedAccessException;

namespace AAS.TwinEngine.DataEngine.UnitTests.Api.AasRegistry;

public class ShellDescriptorControllerTests
{
    private readonly IShellDescriptorHandler _handler;
    private readonly ShellDescriptorController _sut;
    private readonly ShellDescriptorsDto _expectedShellDescriptors;
    private readonly ShellDescriptorDto _expectedShellDescriptor;
    public const string AasId = "https://example.com/ids/aas/1170_1160_3052_6568";

    public ShellDescriptorControllerTests()
    {
        var logger = Substitute.For<ILogger<ShellDescriptorController>>();
        _handler = Substitute.For<IShellDescriptorHandler>();
        _sut = new ShellDescriptorController(logger, _handler);

        _expectedShellDescriptor = CreateShellDescriptor(
            idShort: "SensorWeatherStationExample",
            id: AasId,
            globalAssetId: "https://example.com/ids/F/5350_5407_2522_6562",
            href: "http://localhost:8081/shells/aHR0c..."
        );

        _expectedShellDescriptors = new ShellDescriptorsDto
        {
            PagingMetaData = new PagingMetaDataDto() { Cursor = null },
            Result = [_expectedShellDescriptor]
        };
    }

    [Fact]
    public async Task GetAllShellDescriptorsAsync_ReturnsOkResult()
    {
        _handler.GetAllShellDescriptors(Arg.Any<GetShellDescriptorsRequest>(), Arg.Any<CancellationToken>()).Returns(_expectedShellDescriptors);

        var result = await _sut.GetAllShellDescriptorsAsync(null, null, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var json = Assert.IsType<ShellDescriptorsDto>(okResult.Value);
        Assert.Equal(_expectedShellDescriptors, json);
    }

    [Fact]
    public async Task GetAllShellDescriptorsAsync_ThrowsException_ReturnsInternalServerError()
    {
        _handler.GetAllShellDescriptors(Arg.Any<GetShellDescriptorsRequest>(), Arg.Any<CancellationToken>()).Throws(new InternalServerException("Internal error"));

        var result = await Record.ExceptionAsync(() => _sut.GetAllShellDescriptorsAsync(null, null, CancellationToken.None));

        Assert.NotNull(result);
        Assert.IsType<InternalServerException>(result);
    }

    [Fact]
    public async Task GetAllShellDescriptorsAsync_ReturnsNotFound_WhenHandlerReturnsNull()
    {
        _handler.GetAllShellDescriptors(Arg.Any<GetShellDescriptorsRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<ShellDescriptorsDto>(null!));

        var result = await _sut.GetAllShellDescriptorsAsync(2, null, CancellationToken.None);

        var notFoundResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Null(notFoundResult.Value);
    }

    [Fact]
    public async Task GetAllShellDescriptorsAsync_ThrowsUnauthorizedAccessException_Returns401()
    {
        _handler.GetAllShellDescriptors(Arg.Any<GetShellDescriptorsRequest>(), Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException("Unauthorized"));

        var exception = await Record.ExceptionAsync(() => _sut.GetAllShellDescriptorsAsync(null, null, CancellationToken.None));

        Assert.NotNull(exception);
        Assert.IsType<UnauthorizedAccessException>(exception);
    }

    [Fact]
    public async Task GetShellDescriptorByIdAsync_ReturnsOkResult()
    {
        var encodedId = AasId.EncodeBase64Url();

        _handler.GetShellDescriptorById(Arg.Any<GetShellDescriptorRequest>(), Arg.Any<CancellationToken>()).Returns(_expectedShellDescriptor);

        var result = await _sut.GetShellDescriptorByIdAsync(encodedId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var json = Assert.IsType<ShellDescriptorDto>(okResult.Value);
        Assert.Equal(_expectedShellDescriptor, json);
    }

    [Fact]
    public async Task GetShellDescriptorByIdAsync_ReturnsNotFound_WhenHandlerReturnsNull()
    {
        var encodedId = AasId.EncodeBase64Url();

        _handler.GetShellDescriptorById(Arg.Any<GetShellDescriptorRequest>(), Arg.Any<CancellationToken>()).Returns((ShellDescriptorDto)null!);

        var result = await _sut.GetShellDescriptorByIdAsync(encodedId, CancellationToken.None);

        var notFoundResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Null(notFoundResult.Value);
    }

    [Fact]
    public async Task GetShellDescriptorByIdAsync_ThrowsUnauthorizedAccessException_Returns401()
    {
        var encodedId = AasId.EncodeBase64Url();

        _handler.GetShellDescriptorById(Arg.Any<GetShellDescriptorRequest>(), Arg.Any<CancellationToken>()).Throws(new UnauthorizedAccessException("Unauthorized"));

        var exception = await Record.ExceptionAsync(() => _sut.GetShellDescriptorByIdAsync(encodedId, CancellationToken.None));

        Assert.NotNull(exception);
        Assert.IsType<UnauthorizedAccessException>(exception);
    }

    [Fact]
    public async Task GetShellDescriptorByIdAsync_ThrowsException_ReturnsInternalServerError()
    {
        var encodedId = AasId.EncodeBase64Url();
        _handler.GetShellDescriptorById(Arg.Any<GetShellDescriptorRequest>(), Arg.Any<CancellationToken>()).Throws(new InternalServerException("Internal error"));

        var exception = await Record.ExceptionAsync(() => _sut.GetShellDescriptorByIdAsync(encodedId, CancellationToken.None));

        Assert.NotNull(exception);
        Assert.IsType<InternalServerException>(exception);
    }

    private static ShellDescriptorDto CreateShellDescriptor(string idShort, string id, string globalAssetId, string href)
    {
        return new ShellDescriptorDto
        {
            Description = null!,
            DisplayName = null!,
            Extensions = null!,
            Administration = null!,
            AssetKind = AssetKind.Type,
            AssetType = AssetKind.Type,
            Endpoints =
            [
                new EndpointDto {
                    Interface = "AAS-3.0",
                    ProtocolInformation = new ProtocolInformationDto
                    {
                        Href = href,
                        EndpointProtocol = "http",
                        EndpointProtocolVersion = null!,
                        SubProtocol = null!,
                        SubProtocolBody = null!,
                        SubProtocolBodyEncoding = null!,
                        SecurityAttributes = null!
                    }
                }
            ],
            GlobalAssetId = globalAssetId,
            IdShort = idShort,
            Id = id,
            SpecificAssetIds = null,
            SubmodelDescriptors = null!
        };
    }
}

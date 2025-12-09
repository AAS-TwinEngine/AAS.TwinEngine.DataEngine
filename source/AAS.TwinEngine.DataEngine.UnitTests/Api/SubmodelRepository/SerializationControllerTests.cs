using AAS.TwinEngine.DataEngine.Api.SubmodelRepository;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Handler;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AAS.TwinEngine.DataEngine.UnitTests.Api.SubmodelRepository;

public class SerializationControllerTests
{
    private readonly ISerializationHandler _handler;
    private readonly SerializationController _sut;
    private static readonly string[] AasIds = ["validAasId"];
    private static readonly string[] SubmodelIds = ["validSubmodelId"];

    public SerializationControllerTests()
    {
        var logger = Substitute.For<ILogger<SerializationController>>();
        _handler = Substitute.For<ISerializationHandler>();
        _sut = new SerializationController(logger, _handler);
    }

    [Fact]
    public async Task SerializeAasx_ReturnsFileStreamResult_WhenValidRequest()
    {
        var stream = new MemoryStream("aasx file content"u8.ToArray());
        var expectedResult = new FileStreamResult(stream, "application/asset-administration-shell-package")
        {
            FileDownloadName = $"{AasIds.FirstOrDefault()}.aasx"
        };
        _handler.GetAasxFileAsync(Arg.Any<SerializeAasxRequest>(), Arg.Any<CancellationToken>())
                .Returns(expectedResult);

        var result = await _sut.SerializeAasxAsync(AasIds, SubmodelIds, CancellationToken.None);

        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("application/asset-administration-shell-package", fileResult.ContentType);
        Assert.Equal($"{AasIds.FirstOrDefault()}.aasx", fileResult.FileDownloadName);
    }

    [Fact]
    public async Task SerializeAasx_ThrowsNotFoundException_WhenHandlerThrows()
    {
        _handler.GetAasxFileAsync(Arg.Any<SerializeAasxRequest>(), Arg.Any<CancellationToken>())
                .Throws(new NotFoundException());

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _sut.SerializeAasxAsync(AasIds, SubmodelIds, CancellationToken.None));
    }

    [Fact]
    public async Task SerializeAasx_ThrowsException_WhenHandlerFails()
    {
        _handler.GetAasxFileAsync(Arg.Any<SerializeAasxRequest>(), Arg.Any<CancellationToken>())
                .Throws(new Exception("Unexpected error"));

        await Assert.ThrowsAsync<Exception>(() => _sut.SerializeAasxAsync(AasIds, SubmodelIds, CancellationToken.None));
    }

    [Fact]
    public async Task SerializeAasx_ReturnsBadRequest_WhenSubmodelIdsIsNull()
    {
        _handler.GetAasxFileAsync(Arg.Any<SerializeAasxRequest>(), Arg.Any<CancellationToken>())
                .Throws(new BadRequestException("SubmodelIds must not be null or empty."));

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => _sut.SerializeAasxAsync(AasIds, null!, CancellationToken.None));
        Assert.Equal("SubmodelIds must not be null or empty.", ex.Message);
    }

}

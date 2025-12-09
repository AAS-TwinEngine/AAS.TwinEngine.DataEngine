using AAS.TwinEngine.DataEngine.Api.Shared;
using AAS.TwinEngine.DataEngine.Api.SubmodelRegistry;
using AAS.TwinEngine.DataEngine.Api.SubmodelRegistry.Handler;
using AAS.TwinEngine.DataEngine.Api.SubmodelRegistry.Requests;
using AAS.TwinEngine.DataEngine.Api.SubmodelRegistry.Responses;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using UnauthorizedAccessException = System.UnauthorizedAccessException;

namespace AAS.TwinEngine.DataEngine.UnitTests.Api.SubmodelRegistry;

public class SubmodelDescriptorControllerTests
{
    private readonly ISubmodelDescriptorHandler _handler;
    private readonly SubmodelDescriptorController _sut;
    private readonly SubmodelDescriptorDto _expectedDto;
    public const string SubmodelId = "https://example.com/ids/submodel/1234_5678";

    public SubmodelDescriptorControllerTests()
    {
        var logger = Substitute.For<ILogger<SubmodelDescriptorController>>();
        _handler = Substitute.For<ISubmodelDescriptorHandler>();
        _sut = new SubmodelDescriptorController(logger, _handler);

        _expectedDto = CreateDto(id: SubmodelId,
                                 href: "http://localhost:8075/submodels/aHR0cHM6Ly9leGFtcGxlLmNvbS9pZHMvc3VibW9kZWwvMTIzNF81Njc4");
    }

    [Fact]
    public async Task GetSubmodelDescriptorByIdAsync_ReturnsOkResult()
    {
        var encodedId = SubmodelId.EncodeBase64Url();
        _handler
            .GetSubmodelDescriptorById(Arg.Any<GetSubmodelDescriptorRequest>(), Arg.Any<CancellationToken>())
            .Returns(_expectedDto);

        var result = await _sut.GetSubmodelDescriptorByIdAsync(encodedId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<SubmodelDescriptorDto>(okResult.Value);
        Assert.Equal(_expectedDto, dto);
    }

    [Fact]
    public async Task GetSubmodelDescriptorByIdAsync_ReturnsOkWithNull_WhenHandlerReturnsNull()
    {
        var encodedId = SubmodelId.EncodeBase64Url();
        _handler
            .GetSubmodelDescriptorById(Arg.Any<GetSubmodelDescriptorRequest>(), Arg.Any<CancellationToken>())!
            .Returns((SubmodelDescriptorDto?)null);

        var result = await _sut.GetSubmodelDescriptorByIdAsync(encodedId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Null(okResult.Value);
    }

    [Fact]
    public async Task GetSubmodelDescriptorByIdAsync_ThrowsUnauthorizedAccessException_Returns401()
    {
        var encodedId = SubmodelId.EncodeBase64Url();
        _handler
            .GetSubmodelDescriptorById(Arg.Any<GetSubmodelDescriptorRequest>(), Arg.Any<CancellationToken>())
            .Throws(new UnauthorizedAccessException("Unauthorized"));

        var ex = await Record.ExceptionAsync(() =>
            _sut.GetSubmodelDescriptorByIdAsync(encodedId, CancellationToken.None));

        Assert.IsType<UnauthorizedAccessException>(ex);
    }

    [Fact]
    public async Task GetSubmodelDescriptorByIdAsync_ThrowsException_ReturnsInternalServerError()
    {
        var encodedId = SubmodelId.EncodeBase64Url();
        _handler
            .GetSubmodelDescriptorById(Arg.Any<GetSubmodelDescriptorRequest>(), Arg.Any<CancellationToken>())
            .Throws(new InternalServerException("Internal error"));

        var ex = await Record.ExceptionAsync(() =>
            _sut.GetSubmodelDescriptorByIdAsync(encodedId, CancellationToken.None));

        Assert.IsType<InternalServerException>(ex);
    }

    private static SubmodelDescriptorDto CreateDto(string id, string href)
    {
        return new SubmodelDescriptorDto
        {
            Id = id,
            Endpoints =
            [
                new EndpointDto
                {
                    ProtocolInformation = new ProtocolInformationDto
                    {
                        Href = href
                    }
                }
            ]
        };
    }
}


using AAS.TwinEngine.DataEngine.Api.Description;
using AAS.TwinEngine.DataEngine.Api.Description.Handler;
using AAS.TwinEngine.DataEngine.Api.Description.Responses;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AAS.TwinEngine.DataEngine.UnitTests.Api.Description;

public class DescriptionControllerTests
{
    private readonly IDescriptionHandler _handler;
    private readonly DescriptionController _sut;

    public DescriptionControllerTests()
    {
        var logger = Substitute.For<ILogger<DescriptionController>>();
        _handler = Substitute.For<IDescriptionHandler>();

        _sut = new DescriptionController(logger, _handler);
    }

    [Fact]
    public void GetDescriptor_ReturnsOkResult()
    {
        var expected = new ServiceDescriptionDto
        {
            Profiles =
            [
                "https://admin-shell.io/aas/API/3/1/AssetAdministrationShellRegistryServiceSpecification/SSP-002",
                "https://admin-shell.io/aas/API/3/1/AssetAdministrationShellRepositoryServiceSpecification/SSP-002",
                "https://admin-shell.io/aas/API/3/1/SubmodelRegistryServiceSpecification/SSP-002",
                "https://admin-shell.io/aas/API/3/1/SubmodelRepositoryServiceSpecification/SSP-002",
                "https://admin-shell.io/aas/API/3/1/DiscoveryServiceSpecification/SSP-002"
            ]
        };

        _handler
            .GetDescriptor(Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = _sut.GetDescriptor(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ServiceDescriptionDto>(okResult.Value);

        Assert.Equal(expected, dto);
    }

    [Fact]
    public void GetDescriptor_ReturnsOkWithNull_WhenHandlerReturnsNull()
    {
        _handler
            .GetDescriptor(Arg.Any<CancellationToken>())
            .Returns((ServiceDescriptionDto?)null);

        var result = _sut.GetDescriptor(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);

        Assert.Null(okResult.Value);
    }

    [Fact]
    public void GetDescriptor_ThrowsForbiddenException()
    {
        _handler
            .GetDescriptor(Arg.Any<CancellationToken>())
            .Throws(new ForbiddenException("Forbidden"));

        var ex = Record.Exception(() =>
            _sut.GetDescriptor(CancellationToken.None));

        Assert.IsType<ForbiddenException>(ex);
    }
}

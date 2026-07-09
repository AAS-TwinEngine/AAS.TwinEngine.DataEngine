using AAS.TwinEngine.DataEngine.Api.Description.Handler;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Description;
using AAS.TwinEngine.DataEngine.DomainModel.Description;

using Microsoft.Extensions.Logging;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.Api.Description.Handler;

public class DescriptionHandlerTests
{
    private readonly ILogger<DescriptionHandler> _logger;
    private readonly IDescriptionService _descriptionService;
    private readonly DescriptionHandler _sut;

    public DescriptionHandlerTests()
    {
        _logger = Substitute.For<ILogger<DescriptionHandler>>();
        _descriptionService = Substitute.For<IDescriptionService>();

        _sut = new DescriptionHandler(_logger, _descriptionService);
    }

    [Fact]
    public void GetDescriptor_ReturnsExpectedDto()
    {
        var descriptor = new ServiceDescription
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

        _descriptionService
            .GetDescriptor(Arg.Any<CancellationToken>())
            .Returns(descriptor);

        var result = _sut.GetDescriptor(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(descriptor.Profiles, result.Profiles);
    }

    [Fact]
    public void GetDescriptor_CallsServiceOnce()
    {
        _descriptionService
            .GetDescriptor(Arg.Any<CancellationToken>())
            .Returns(new ServiceDescription());

        _sut.GetDescriptor(CancellationToken.None);

        _descriptionService.Received(1)
            .GetDescriptor(CancellationToken.None);
    }
}

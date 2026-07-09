using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Description;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Services.Description;

public class DescriptionServiceTests
{
    private static readonly string[] ExpectedProfiles =
    [
        "https://admin-shell.io/aas/API/3/1/AssetAdministrationShellRegistryServiceSpecification/SSP-002",
        "https://admin-shell.io/aas/API/3/1/AssetAdministrationShellRepositoryServiceSpecification/SSP-002",
        "https://admin-shell.io/aas/API/3/1/SubmodelRegistryServiceSpecification/SSP-002",
        "https://admin-shell.io/aas/API/3/1/SubmodelRepositoryServiceSpecification/SSP-002",
        "https://admin-shell.io/aas/API/3/1/DiscoveryServiceSpecification/SSP-002"
    ];

    [Fact]
    public void GetDescriptor_WhenCalled_ReturnsAllFiveSspProfiles()
    {
        var sut = new DescriptionService();

        var result = sut.GetDescriptor(CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Profiles);
        Assert.Equal(5, result.Profiles!.Count);
        Assert.Equal(ExpectedProfiles, result.Profiles);
    }
}

using AAS.TwinEngine.DataEngine.Api.Description.MappingProfiles;
using AAS.TwinEngine.DataEngine.DomainModel.Description;

namespace AAS.TwinEngine.DataEngine.UnitTests.Api.Description.MappingProfiles;

public class ServiceDescriptionMapperProfileTests
{
    [Fact]
    public void ToDto_WithProfiles_ReturnsMappedDto()
    {
        var serviceDescription = new ServiceDescription
        {
            Profiles =
            [
                "https://admin-shell.io/aas/API/3/1/AssetAdministrationShellRegistryServiceSpecification/SSP-002",
                "https://admin-shell.io/aas/API/3/1/SubmodelRegistryServiceSpecification/SSP-002"
            ]
        };

        var result = serviceDescription.ToDto();

        Assert.NotNull(result);
        Assert.Equal(serviceDescription.Profiles, result.Profiles);
    }

    [Fact]
    public void ToDto_WithNullProfiles_ReturnsEmptyProfilesCollection()
    {
        var serviceDescription = new ServiceDescription
        {
            Profiles = null
        };

        var result = serviceDescription.ToDto();

        Assert.NotNull(result);
        Assert.NotNull(result.Profiles);
        Assert.Empty(result.Profiles);
    }
}

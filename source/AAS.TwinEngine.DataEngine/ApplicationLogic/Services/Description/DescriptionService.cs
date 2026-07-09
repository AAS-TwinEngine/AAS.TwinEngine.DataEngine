using AAS.TwinEngine.DataEngine.DomainModel.Description;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Description;

public class DescriptionService : IDescriptionService
{
    private static readonly string[] SupportedProfiles =
    [
        "https://admin-shell.io/aas/API/3/1/AssetAdministrationShellRegistryServiceSpecification/SSP-002",
        "https://admin-shell.io/aas/API/3/1/AssetAdministrationShellRepositoryServiceSpecification/SSP-002",
        "https://admin-shell.io/aas/API/3/1/SubmodelRegistryServiceSpecification/SSP-002",
        "https://admin-shell.io/aas/API/3/1/SubmodelRepositoryServiceSpecification/SSP-002",
        "https://admin-shell.io/aas/API/3/1/DiscoveryServiceSpecification/SSP-002"
    ];

    public ServiceDescription GetDescriptor(CancellationToken cancellationToken) => new() { Profiles = SupportedProfiles };
}

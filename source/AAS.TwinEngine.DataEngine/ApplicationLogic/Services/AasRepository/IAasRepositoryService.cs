using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.AasRepository;
using AAS.TwinEngine.DataEngine.DomainModel.Discovery;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;

public interface IAasRepositoryService
{
    Task<Shells> GetShellsByFiltersAsync(ShellSearchFilter? filter, int? limit, string? cursor, CancellationToken cancellationToken);

    Task<IAssetAdministrationShell?> GetShellByIdAsync(string aasIdentifier, CancellationToken cancellationToken);

    Task<IAssetInformation> GetAssetInformationByIdAsync(string aasIdentifier, CancellationToken cancellationToken);

    Task<SubmodelRef> GetSubmodelRefByIdAsync(string aasIdentifier, int? limit, string? cursor, CancellationToken cancellationToken);
}

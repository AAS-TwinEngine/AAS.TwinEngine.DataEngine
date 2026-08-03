using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.AasRepository;
using AAS.TwinEngine.DataEngine.DomainModel.Discovery;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;

public interface IAasRepositoryService
{
    Task<Shells> GetShellsByFiltersAsync(ShellSearchFilter? filter, int? limit, string? cursor, CancellationToken cancellationToken);

    Task<IAssetAdministrationShell?> GetShellByIdAsync(string aasIdentifier, CancellationToken cancellationToken);

    Task<IAssetInformation> GetAssetInformationByIdAsync(string aasIdentifier, CancellationToken cancellationToken);

    Task<SubmodelRef> GetSubmodelRefByIdAsync(string aasIdentifier, int? limit, string? cursor, CancellationToken cancellationToken);

    Task<bool> IsSubmodelReferencedByAasAsync(string aasIdentifier, string submodelIdentifier, CancellationToken cancellationToken);

    Task<ISubmodel> GetSubmodelByAasIdAsync(string aasId, string submodelId, Level level, Extent extent, CancellationToken cancellationToken);

    Task<SubmodelElementsPage> GetAllSubmodelElementsByAasIdAsync(string aasId, string submodelId, Level level, Extent extent, int? limit, string? cursor, CancellationToken cancellationToken);

    Task<ISubmodelElement> GetSubmodelElementByAasIdAsync(string aasId, string submodelId, string idShortPath, CancellationToken cancellationToken);
}

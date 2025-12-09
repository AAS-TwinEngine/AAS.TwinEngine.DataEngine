using AAS.TwinEngine.DataEngine.DomainModel.AasRepository;

using AasCore.Aas3_0;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;

public interface IAasRepositoryService
{
    Task<IAssetAdministrationShell?> GetShellByIdAsync(string aasIdentifier, CancellationToken cancellationToken);

    Task<IAssetInformation> GetAssetInformationByIdAsync(string aasIdentifier, CancellationToken cancellationToken);

    Task<SubmodelRef> GetSubmodelRefByIdAsync(string aasIdentifier, int? limit, string? cursor, CancellationToken cancellationToken);
}

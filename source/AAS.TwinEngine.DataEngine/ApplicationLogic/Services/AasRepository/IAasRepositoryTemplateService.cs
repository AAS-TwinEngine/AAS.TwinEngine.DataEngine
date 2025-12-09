using AasCore.Aas3_0;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;

public interface IAasRepositoryTemplateService
{
    Task<IAssetAdministrationShell> GetShellTemplateAsync(string aasIdentifier, CancellationToken cancellationToken);

    Task<IAssetInformation> GetAssetInformationTemplateAsync(string aasIdentifier, CancellationToken cancellationToken);

    Task<List<IReference>> GetSubmodelRefByIdAsync(string aasIdentifier, CancellationToken cancellationToken);
}

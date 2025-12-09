using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;

using AasCore.Aas3_0;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;

public interface ITemplateProvider
{
    Task<ISubmodel> GetSubmodelTemplateAsync(string templateId, CancellationToken cancellationToken);

    Task<ShellDescriptor> GetShellDescriptorsTemplateAsync(CancellationToken cancellationToken);

    Task<IAssetAdministrationShell> GetShellTemplateAsync(string templateId, CancellationToken cancellationToken);

    Task<IAssetInformation> GetAssetInformationTemplateAsync(string templateId, CancellationToken cancellationToken);

    Task<List<IReference>> GetSubmodelRefByIdAsync(string templateId, CancellationToken cancellationToken);

    Task<IConceptDescription?> GetConceptDescriptionByIdAsync(string cdIdentifier, CancellationToken cancellationToken);
}

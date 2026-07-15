using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;

public interface ITemplateProvider
{
    Task<ISubmodel?> GetFilteredSubmodelTemplateAsync(string templateId, SubmodelQueryOptions? queryOptions, CancellationToken cancellationToken);

    Task<ISubmodel?> GetFilteredSubmodelTemplateBySemanticIdAsync(string semanticId, CancellationToken cancellationToken);

    Task<ShellDescriptor> GetShellDescriptorTemplateAsync(string templateId, CancellationToken cancellationToken);

    Task<IAssetAdministrationShell> GetShellTemplateAsync(string templateId, CancellationToken cancellationToken);

    Task<IAssetInformation> GetAssetInformationTemplateAsync(string templateId, CancellationToken cancellationToken);

    Task<List<IReference>> GetSubmodelRefByIdAsync(string templateId, CancellationToken cancellationToken);

    Task<IConceptDescription?> GetConceptDescriptionByIdAsync(string cdIdentifier, CancellationToken cancellationToken);
}

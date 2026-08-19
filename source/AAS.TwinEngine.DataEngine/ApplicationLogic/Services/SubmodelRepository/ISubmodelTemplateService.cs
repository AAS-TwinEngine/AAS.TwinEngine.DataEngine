using AAS.TwinEngine.DataEngine.DomainModel.Shared;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;

public interface ISubmodelTemplateService
{
    Task<ISubmodel> GetSubmodelTemplateAsync(string submodelId, CancellationToken cancellationToken);

    Task<ISubmodel> GetSubmodelTemplateAsync(string submodelId, string idShortPath, SubmodelQueryOptions? queryOptions, CancellationToken cancellationToken);

    Task<ISubmodel?> GetFilteredSubmodelTemplateAsync(string submodelId, string filteredTemplateId, SubmodelQueryOptions? queryOptions, CancellationToken cancellationToken);

    Task<string?> GetFilteredSubmodelTemplateIdAsync(string semanticId, CancellationToken cancellationToken);
}

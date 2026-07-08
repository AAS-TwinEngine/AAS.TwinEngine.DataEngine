using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;

public interface ISubmodelRepositoryService
{
    Task<ISubmodel> GetSubmodelAsync(string submodelId, CancellationToken cancellationToken);

    Task<ISubmodelElement> GetSubmodelElementAsync(string submodelId, string idShortPath, CancellationToken cancellationToken);

    Task<SubmodelList> GetAllSubmodelsAsync(
        SubmodelSearchFilter? filter,
        SubmodelQueryOptions? queryOptions,
        int? limit,
        string? cursor,
        CancellationToken cancellationToken);
}

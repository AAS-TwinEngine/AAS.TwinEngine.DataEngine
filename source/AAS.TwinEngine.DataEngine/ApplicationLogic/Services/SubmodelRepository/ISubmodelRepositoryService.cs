using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;

public interface ISubmodelRepositoryService
{
    Task<ISubmodel> GetSubmodelAsync(string submodelId, SubmodelQueryOptions? queryOptions, CancellationToken cancellationToken);

    Task<ISubmodelElement> GetSubmodelElementAsync(string submodelId, string idShortPath, CancellationToken cancellationToken);

    Task<SubmodelList> GetAllSubmodelsAsync(
        SubmodelSearchFilter? filter,
        SubmodelQueryOptions? queryOptions,
        int? limit,
        string? cursor,
        CancellationToken cancellationToken);

    Task<SubmodelElementsPage> GetAllSubmodelElementsAsync(
        string submodelId,
        SubmodelQueryOptions? queryOptions,
        int? limit,
        string? cursor,
        CancellationToken cancellationToken);

    Task<FileAttachmentResult> GetFileAttachmentAsync(string submodelId, string idShortPath, CancellationToken cancellationToken);
}

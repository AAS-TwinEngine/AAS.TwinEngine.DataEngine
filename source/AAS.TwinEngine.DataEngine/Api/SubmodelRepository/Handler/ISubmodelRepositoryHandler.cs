using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Responses;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Handler;

public interface ISubmodelRepositoryHandler
{
    Task<ISubmodel> GetSubmodel(GetSubmodelRequest request, CancellationToken cancellationToken);

    Task<ISubmodelElement> GetSubmodelElement(GetSubmodelElementRequest request, CancellationToken cancellationToken);

    Task<SubmodelsDto> GetAllSubmodels(GetAllSubmodelsRequest request, CancellationToken cancellationToken);

    Task<SubmodelElementsDto> GetAllSubmodelElements(GetAllSubmodelElementsRequest request, CancellationToken cancellationToken);

    Task GetFileAttachment(GetSubmodelElementRequest request, CancellationToken cancellationToken);
}

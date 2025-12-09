using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;

using AasCore.Aas3_0;

namespace AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Handler;

public interface ISubmodelRepositoryHandler
{
    Task<ISubmodel> GetSubmodel(GetSubmodelRequest request, CancellationToken cancellationToken);

    Task<ISubmodelElement> GetSubmodelElement(GetSubmodelElementRequest request, CancellationToken cancellationToken);
}

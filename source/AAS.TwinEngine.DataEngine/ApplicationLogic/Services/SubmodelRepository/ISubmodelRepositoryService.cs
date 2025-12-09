using AasCore.Aas3_0;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;

public interface ISubmodelRepositoryService
{
    Task<ISubmodel> GetSubmodelAsync(string submodelId, CancellationToken cancellationToken);

    Task<ISubmodelElement> GetSubmodelElementAsync(string submodelId, string idShortPath, CancellationToken cancellationToken);
}

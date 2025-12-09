using AasCore.Aas3_0;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;

public interface ISubmodelTemplateService
{
    Task<ISubmodel> GetSubmodelTemplateAsync(string submodelId, CancellationToken cancellationToken);

    Task<ISubmodel> GetSubmodelTemplateAsync(string submodelId, string idShortPath, CancellationToken cancellationToken);
}

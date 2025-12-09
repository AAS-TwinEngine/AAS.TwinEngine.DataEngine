using AasCore.Aas3_0;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;

public interface IConceptDescriptionService
{
    Task<IConceptDescription?> GetConceptDescriptionById(string cdIdentifier, CancellationToken cancellationToken);
}

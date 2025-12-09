using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;

using AasCore.Aas3_0;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;

public class ConceptDescriptionService(ITemplateProvider templateProvider,
                                       ILogger<ConceptDescriptionService> logger) : IConceptDescriptionService
{
    public Task<IConceptDescription?> GetConceptDescriptionById(string cdIdentifier, CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching concept description by ID: {CdIdentifier}", cdIdentifier);
        var response = templateProvider.GetConceptDescriptionByIdAsync(cdIdentifier, cancellationToken);
        return response;
    }
}

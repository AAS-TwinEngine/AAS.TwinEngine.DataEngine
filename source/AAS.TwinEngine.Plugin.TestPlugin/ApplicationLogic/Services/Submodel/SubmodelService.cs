using Aas.TwinEngine.Plugin.TestPlugin.DomainModel.Submodel;

namespace Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.Submodel;

public class SubmodelService(
    ISubmodelProvider submodelProvider
    ) : ISubmodelService
{
    public Task<SemanticTreeNode> GetValuesBySemanticIds(SemanticTreeNode semanticIds, string submodelId) => Task.FromResult(submodelProvider.EnrichWithData(semanticIds, submodelId));
}

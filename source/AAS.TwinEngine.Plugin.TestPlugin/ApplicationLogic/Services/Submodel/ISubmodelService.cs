using Aas.TwinEngine.Plugin.TestPlugin.DomainModel.Submodel;

namespace Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.Submodel;

/// <summary>
/// Retrieves product data based on a complex data query and returns it in a structured format.
/// </summary>
public interface ISubmodelService
{
    public Task<SemanticTreeNode> GetValuesBySemanticIds(SemanticTreeNode semanticIds, string submodelId);
}

using Aas.TwinEngine.Plugin.TestPlugin.DomainModel.Submodel;

namespace Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.Submodel;

/// <summary>
/// Provides functionality to enrich a semantic tree node with data.
/// </summary>
public interface ISubmodelProvider
{
    public SemanticTreeNode EnrichWithData(SemanticTreeNode semanticTreeNode, string submodelId);
}

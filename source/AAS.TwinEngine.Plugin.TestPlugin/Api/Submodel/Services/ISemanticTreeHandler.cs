using System.Text.Json.Nodes;

using AAS.TwinEngine.Plugin.TestPlugin.DomainModel.Submodel;

using Json.Schema;

namespace AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Services;

/// <summary>
/// Converts a semantic tree node with values into a structured JSON format.
/// </summary>
public interface ISemanticTreeHandler
{
    JsonObject GetJson(SemanticTreeNode semanticTreeNodeWithValues, JsonSchema dataQuery);
}

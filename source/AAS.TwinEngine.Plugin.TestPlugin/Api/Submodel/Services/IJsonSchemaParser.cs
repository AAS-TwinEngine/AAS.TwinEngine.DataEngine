using Aas.TwinEngine.Plugin.TestPlugin.DomainModel.Submodel;

using Json.Schema;

namespace Aas.TwinEngine.Plugin.TestPlugin.Api.Submodel.Services;

/// <summary>
/// Parses a complex JSON schema and converts it into a semantic tree structure.
/// </summary>
public interface IJsonSchemaParser
{
    SemanticTreeNode ParseJsonSchema(JsonSchema jsonSchema);
}

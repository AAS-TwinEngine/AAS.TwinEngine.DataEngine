using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using Json.Schema;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Helper;

public interface IPluginSchemaCompatibilityHandler
{
    JsonSchema GenerateSchema(SemanticTreeNode semanticNode);

    Task<IList<HttpContent>> RetryWithLegacySchemaAsync(
        IDictionary<string, SemanticTreeNode> semanticNodes,
        string submodelId,
        CancellationToken cancellationToken);
}

using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Helper;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Helper.Legacy;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Helper.LegacyV1;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using Json.Schema;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Helper;

public sealed class PluginSchemaCompatibilityHandler(
    IJsonSchemaGenerator jsonSchemaGenerator,
    ILegacySchemaRetryHandler legacySchemaRetryHandler) : IPluginSchemaCompatibilityHandler
{
    public JsonSchema GenerateSchema(SemanticTreeNode semanticNode)
        => jsonSchemaGenerator.Generate(semanticNode);

    public Task<IList<HttpContent>> RetryWithLegacySchemaAsync(
        IDictionary<string, SemanticTreeNode> semanticNodes,
        string submodelId,
        CancellationToken cancellationToken)
        => legacySchemaRetryHandler.RetryWithDraft7Async(semanticNodes, submodelId, cancellationToken);
}

using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Helper.Legacy;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Providers;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Helper.LegacyV1;

#pragma warning disable S1133
[Obsolete("Draft-07 schema retry is deprecated and will be removed in the next major release. All plugins should support Draft 2020-12.")]
public sealed class LegacySchemaRetryHandler(
    IPluginRequestBuilder pluginRequestBuilder,
    IPluginDataProvider pluginDataProvider,
    ILogger<LegacySchemaRetryHandler> logger) : ILegacySchemaRetryHandler
{
    private readonly LegacyDraft7JsonSchemaGenerator _generator = new();

    public async Task<IList<HttpContent>> RetryWithDraft7Async(
        IDictionary<string, SemanticTreeNode> semanticNodes,
        string submodelId,
        CancellationToken cancellationToken)
    {
        logger.LogWarning(
            "Plugin rejected Draft 2020-12 schema (HTTP 400 or 500). Retrying with Draft-07 fallback. " +
            "This is a legacy compatibility path scheduled for removal in the next major release.");

        var draft7Schemas = semanticNodes.ToDictionary(
            kvp => kvp.Key,
            kvp => _generator.Generate(kvp.Value));

        var pluginRequests = pluginRequestBuilder.Build(draft7Schemas);

        return await pluginDataProvider
            .GetDataForSemanticIdsAsync(pluginRequests, submodelId, cancellationToken)
            .ConfigureAwait(false);
    }
}
#pragma warning restore S1133

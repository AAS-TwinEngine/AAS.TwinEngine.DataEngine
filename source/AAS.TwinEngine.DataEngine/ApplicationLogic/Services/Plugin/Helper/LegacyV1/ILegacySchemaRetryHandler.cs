using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Helper.Legacy;

/// <summary>
/// Retries a plugin data request using a Draft-07 JSON Schema when the plugin
/// rejects the default Draft 2020-12 schema with HTTP 400, 404, or 500.
/// </summary>
/// <remarks>
/// This is a legacy compatibility path. It will be removed in the next major release
/// once all plugins support Draft 2020-12.
/// </remarks>
public interface ILegacySchemaRetryHandler
{
    Task<IList<HttpContent>> RetryWithDraft7Async(
        IDictionary<string, SemanticTreeNode> semanticNodes,
        string submodelId,
        CancellationToken cancellationToken);
}

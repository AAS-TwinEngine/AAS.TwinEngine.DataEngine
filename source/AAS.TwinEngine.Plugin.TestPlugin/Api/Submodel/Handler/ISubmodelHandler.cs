using System.Text.Json.Nodes;

using AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Requests;
using AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Responses;

namespace AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Handler;

public interface ISubmodelHandler
{
    Task<JsonObject> GetSubmodelData(GetSubmodelDataRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<SubmodelDataBatchResult>> GetSubmodelDataBatch(
        IReadOnlyList<GetSubmodelDataBatchRequest> requests,
        CancellationToken cancellationToken);
}

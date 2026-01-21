using System.Text.Json.Nodes;

using AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Requests;

namespace AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Handler;

public interface ISubmodelHandler
{
    Task<JsonObject> GetSubmodelData(GetSubmodelDataRequest request, CancellationToken cancellationToken);
}

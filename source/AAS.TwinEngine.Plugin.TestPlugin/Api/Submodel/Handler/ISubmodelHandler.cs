using System.Text.Json.Nodes;

using Aas.TwinEngine.Plugin.TestPlugin.Api.Submodel.Requests;

namespace Aas.TwinEngine.Plugin.TestPlugin.Api.Submodel.Handler;

public interface ISubmodelHandler
{
    Task<JsonObject> GetSubmodelData(GetSubmodelDataRequest request, CancellationToken cancellationToken);
}

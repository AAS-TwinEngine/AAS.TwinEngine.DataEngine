using Json.Schema;

namespace Aas.TwinEngine.Plugin.TestPlugin.Api.Submodel.Requests;

public record GetSubmodelDataRequest(string submodelId, JsonSchema dataQuery);

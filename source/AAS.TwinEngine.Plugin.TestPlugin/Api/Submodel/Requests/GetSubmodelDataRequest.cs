using Json.Schema;

namespace AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Requests;

public record GetSubmodelDataRequest(string submodelId, JsonSchema dataQuery);

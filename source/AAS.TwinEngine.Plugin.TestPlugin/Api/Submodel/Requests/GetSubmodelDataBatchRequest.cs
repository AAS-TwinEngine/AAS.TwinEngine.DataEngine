using Json.Schema;

namespace AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Requests;

public record GetSubmodelDataBatchRequest(IReadOnlyList<string> SubmodelIds, JsonSchema Schema);
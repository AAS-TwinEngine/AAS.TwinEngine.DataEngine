using System.Text.Json.Nodes;

namespace AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Responses;

public record SubmodelDataBatchResult(string SubmodelId, JsonObject Result);
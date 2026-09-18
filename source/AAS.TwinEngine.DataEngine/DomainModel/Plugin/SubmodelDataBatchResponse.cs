using System.Text.Json;

namespace AAS.TwinEngine.DataEngine.DomainModel.Plugin;

public record SubmodelDataBatchResponse(string SubmodelId, JsonElement Result);
using Json.Schema;

namespace AAS.TwinEngine.DataEngine.DomainModel.Plugin;

public record SubmodelDataBatchRequestGroup(IReadOnlyList<string> SubmodelIds, JsonSchema Schema);
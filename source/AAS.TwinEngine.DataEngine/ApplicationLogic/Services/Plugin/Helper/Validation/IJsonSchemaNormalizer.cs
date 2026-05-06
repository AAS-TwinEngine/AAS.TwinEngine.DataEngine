using System.Text.Json.Nodes;

using Json.Schema;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Helper.Validation;

public interface IJsonSchemaNormalizer
{
    bool TryNormalizeSchema(JsonSchema schema, string targetMetaSchemaId, out JsonSchema normalizedSchema, out string? error);

    void EscapeJsonReferencePointers(JsonNode? currentNode);
}

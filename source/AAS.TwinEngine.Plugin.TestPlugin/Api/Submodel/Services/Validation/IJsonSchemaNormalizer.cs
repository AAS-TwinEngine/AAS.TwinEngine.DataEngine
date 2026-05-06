using System.Diagnostics.CodeAnalysis;

using Json.Schema;

namespace AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Services.Validation;

public interface IJsonSchemaNormalizer
{
    bool TryNormalizeSchema(JsonSchema schema, string targetMetaSchemaId, [NotNullWhen(true)] out JsonSchema? normalizedSchema, out string? error);
}

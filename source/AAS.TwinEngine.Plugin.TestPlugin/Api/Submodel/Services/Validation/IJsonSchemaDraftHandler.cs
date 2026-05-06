using Json.Schema;

namespace AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Services.Validation;

public interface IJsonSchemaDraftHandler
{
    string DraftName { get; }

    string MetaSchemaId { get; }

    JsonSchema MetaSchema { get; }

    IReadOnlyCollection<string> SupportedRefPrefixes { get; }

    bool CanHandle(string? declaredSchema);
}

using Json.Schema;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Helper.Validation;

public interface IJsonSchemaDraftHandler
{
    string DraftName { get; }
    string MetaSchemaId { get; }
    JsonSchema MetaSchema { get; }
    IReadOnlyList<string> SupportedRefPrefixes { get; }

    bool CanHandle(string? declaredSchema);
}

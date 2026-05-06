using Json.Schema;

namespace AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Services.Validation;

public sealed class JsonSchemaDraft202012Handler : IJsonSchemaDraftHandler
{
    private static readonly IReadOnlyCollection<string> RefPrefixes = ["#/$defs/"];

    public string DraftName => "Draft 2020-12";

    public string MetaSchemaId => MetaSchemas.Draft202012Id.OriginalString;

    public JsonSchema MetaSchema => MetaSchemas.Draft202012;

    public IReadOnlyCollection<string> SupportedRefPrefixes => RefPrefixes;

    public bool CanHandle(string? declaredSchema)
    {
        return string.IsNullOrWhiteSpace(declaredSchema)
            || string.Equals(declaredSchema, MetaSchemaId, StringComparison.OrdinalIgnoreCase);
    }
}

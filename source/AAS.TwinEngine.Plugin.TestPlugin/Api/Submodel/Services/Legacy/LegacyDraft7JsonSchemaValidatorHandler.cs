using AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Services.Validation;

using Json.Schema;

namespace AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Services.Legacy;

[Obsolete("Draft-07 support is legacy and should be removed in the next major release.")]
public sealed class LegacyDraft7JsonSchemaValidatorHandler : IJsonSchemaDraftHandler
{
    private static readonly IReadOnlyCollection<string> RefPrefixes = ["#/definitions/"];

    public string DraftName => "Draft-07";

    public string MetaSchemaId => MetaSchemas.Draft7Id.OriginalString;

    public JsonSchema MetaSchema => MetaSchemas.Draft7;

    public IReadOnlyCollection<string> SupportedRefPrefixes => RefPrefixes;

    public bool CanHandle(string? declaredSchema)
    {
        return string.Equals(declaredSchema, MetaSchemaId, StringComparison.OrdinalIgnoreCase);
    }
}
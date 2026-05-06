using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Helper.Validation;

using Json.Schema;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Helper.LegacyV1;

#pragma warning disable S1133
[Obsolete("Draft-07 schema validation is deprecated and will be removed in the next major release. All plugins should support Draft 2020-12.")]
public sealed class LegacyDraft7JsonSchemaValidatorHandler : IJsonSchemaDraftHandler
{
    private static readonly IReadOnlyList<string> RefPrefixes = ["#/definitions/"];

    public string DraftName => "Draft-07";
    public string MetaSchemaId => MetaSchemas.Draft7Id.OriginalString;
    public JsonSchema MetaSchema => MetaSchemas.Draft7;
    public IReadOnlyList<string> SupportedRefPrefixes => RefPrefixes;

    public bool CanHandle(string? declaredSchema)
    {
        if (string.IsNullOrWhiteSpace(declaredSchema))
        {
            return false;
        }

        return string.Equals(declaredSchema, MetaSchemaId, StringComparison.OrdinalIgnoreCase)
            || declaredSchema.Contains("draft-07", StringComparison.OrdinalIgnoreCase);
    }
}

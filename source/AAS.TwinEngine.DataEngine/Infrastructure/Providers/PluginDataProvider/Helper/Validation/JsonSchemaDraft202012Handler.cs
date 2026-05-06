using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Helper.Validation;

using Json.Schema;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Helper.Validation;

public sealed class JsonSchemaDraft202012Handler : IJsonSchemaDraftHandler
{
    private static readonly IReadOnlyList<string> RefPrefixes = ["#/$defs/"];

    public string DraftName => "Draft 2020-12";
    public string MetaSchemaId => MetaSchemas.Draft202012Id.OriginalString;
    public JsonSchema MetaSchema => MetaSchemas.Draft202012;
    public IReadOnlyList<string> SupportedRefPrefixes => RefPrefixes;

    public bool CanHandle(string? declaredSchema)
    {
        if (string.IsNullOrWhiteSpace(declaredSchema))
        {
            return true;
        }

        return string.Equals(declaredSchema, MetaSchemaId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(declaredSchema, MetaSchemas.Draft202012Id.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}

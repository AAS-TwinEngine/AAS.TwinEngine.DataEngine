using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using Json.Schema;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Helper.LegacyV1;

#pragma warning disable S1133
[Obsolete("Draft-07 schema generation is deprecated and will be removed in the next major release. Use JsonSchemaDraft202012Generator instead.")]
public sealed class LegacyDraft7JsonSchemaGenerator : JsonSchemaGeneratorBase
{
    private const string Draft7RefPrefix = "#/definitions/";

    protected override string RefPrefix => Draft7RefPrefix;

    protected override JsonSchema BuildRootSchema(SemanticTreeNode rootNode, JsonSchemaBuilder rootBuilder, Dictionary<string, JsonSchemaBuilder> defs)
    {
        return new JsonSchemaBuilder()
            .Schema(MetaSchemas.Draft7Id)
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchemaBuilder> { [rootNode.SemanticId] = rootBuilder })
            .Definitions(defs)
            .Build();
    }

    protected override JsonSchemaBuilder BuildArraySchema(Dictionary<string, JsonSchemaBuilder> children, List<string> requiredProperties)
    {
        return new JsonSchemaBuilder()
            .Type(SchemaValueType.Array)
            .Properties(children)
            .Required(requiredProperties);
    }
}

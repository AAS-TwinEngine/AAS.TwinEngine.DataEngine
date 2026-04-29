using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using Json.Schema;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Helper;

public sealed class JsonSchemaDraft202012Generator : JsonSchemaGeneratorBase
{
    private const string Draft202012RefPrefix = "#/$defs/";

    protected override string RefPrefix => Draft202012RefPrefix;

    protected override JsonSchema BuildRootSchema(SemanticTreeNode rootNode, JsonSchemaBuilder rootBuilder, Dictionary<string, JsonSchemaBuilder> defs)
    {
        return new JsonSchemaBuilder()
            .Schema(MetaSchemas.Draft202012Id)
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchemaBuilder> { [rootNode.SemanticId] = rootBuilder })
            .Defs(defs)
            .Build();
    }

    protected override JsonSchemaBuilder BuildArraySchema(Dictionary<string, JsonSchemaBuilder> children, List<string> requiredProperties)
    {
        var itemsBuilder = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(children)
            .Required(requiredProperties);

        return new JsonSchemaBuilder()
            .Type(SchemaValueType.Array)
            .Items(itemsBuilder);
    }
}

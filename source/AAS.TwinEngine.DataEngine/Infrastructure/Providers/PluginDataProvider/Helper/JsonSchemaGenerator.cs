using System.Collections.ObjectModel;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using Json.Schema;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Helper;

public static class JsonSchemaGenerator
{
    private const string DefinitionsRefPrefix = "#/definitions/";

    public static JsonSchema ConvertToJsonSchema(SemanticTreeNode rootNode)
    {
        var definitions = new Dictionary<string, JsonSchema>();
        var rootSchema = BuildNode(rootNode, definitions, isRoot: true);

        return new JsonSchemaBuilder()
            .Schema(MetaSchemas.Draft7Id)
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchema>
            {
                [rootNode?.SemanticId!] = rootSchema
            })
            .Definitions(definitions)
            .Build();
    }

    private static JsonSchema BuildNode(SemanticTreeNode node, Dictionary<string, JsonSchema> definitions, bool isRoot = false)
    {
        return node switch
        {
            SemanticBranchNode branch => BuildBranch(branch, definitions, isRoot),
            SemanticLeafNode leaf => BuildLeaf(leaf),
            _ => throw new InternalDataProcessingException()
        };
    }

    private static JsonSchema BuildBranch(SemanticBranchNode branch, Dictionary<string, JsonSchema> definitions, bool isRoot = false)
    {
        if (!isRoot && definitions.ContainsKey(branch.SemanticId))
        {
            return CreateRefSchema(branch.SemanticId);
        }

        var requiredProperties = new List<string>();

        var children = BuildChildren(branch.Children, definitions, requiredProperties);

        var isArray = IsArrayCardinality(branch.Cardinality);

        var schemaBuilder = new JsonSchemaBuilder()
            .Type(isArray ? SchemaValueType.Array : SchemaValueType.Object)
            .Properties(children)
            .Required(requiredProperties);

        if (isRoot)
        {
            return schemaBuilder.Build();
        }

        definitions[branch.SemanticId] = schemaBuilder.Build();
        return CreateRefSchema(branch.SemanticId);
    }

    private static Dictionary<string, JsonSchema> BuildChildren(
        ReadOnlyCollection<SemanticTreeNode> children,
        Dictionary<string, JsonSchema> definitions,
        List<string> required)
    {
        var properties = new Dictionary<string, JsonSchema>();

        foreach (var child in children)
        {
            var childSchema = child switch
            {
                SemanticBranchNode branch => BuildNode(branch, definitions),
                SemanticLeafNode leaf => BuildLeaf(leaf),
                _ => throw new InternalDataProcessingException()
            };

            properties[child.SemanticId] = childSchema;

            if (IsRequiredCardinality(child.Cardinality))
            {
                required.Add(child.SemanticId);
            }
        }

        return properties;
    }

    private static JsonSchema BuildLeaf(SemanticLeafNode leaf)
    {
        var type = leaf.DataType switch
        {
            DataType.String => SchemaValueType.String,
            DataType.Boolean => SchemaValueType.Boolean,
            DataType.Integer => SchemaValueType.Integer,
            DataType.Number => SchemaValueType.Number,
            DataType.Unknown => SchemaValueType.String,
            _ => SchemaValueType.String
        };

        return new JsonSchemaBuilder().Type(type).Build();
    }

    private static bool IsArrayCardinality(Cardinality cardinality) => cardinality is Cardinality.ZeroToMany or Cardinality.OneToMany;

    private static bool IsRequiredCardinality(Cardinality cardinality) => cardinality is Cardinality.One or Cardinality.OneToMany;

    private static JsonSchema CreateRefSchema(string semanticId) => new JsonSchemaBuilder().Ref($"{DefinitionsRefPrefix}{semanticId}").Build();
}

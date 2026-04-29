using System.Collections.ObjectModel;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Helper;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using Json.Schema;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Helper;

public abstract class JsonSchemaGeneratorBase : IJsonSchemaGenerator
{
    protected abstract string RefPrefix { get; }

    public JsonSchema Generate(SemanticTreeNode rootNode)
    {
        var defs = new Dictionary<string, JsonSchemaBuilder>();
        var rootBuilder = BuildNode(rootNode, defs, isRoot: true);
        return BuildRootSchema(rootNode, rootBuilder, defs);
    }

    protected abstract JsonSchema BuildRootSchema(SemanticTreeNode rootNode, JsonSchemaBuilder rootBuilder, Dictionary<string, JsonSchemaBuilder> defs);

    protected abstract JsonSchemaBuilder BuildArraySchema(Dictionary<string, JsonSchemaBuilder> children, List<string> requiredProperties);

    private JsonSchemaBuilder BuildNode(SemanticTreeNode node, Dictionary<string, JsonSchemaBuilder> defs, bool isRoot = false)
    {
        return node switch
        {
            SemanticBranchNode branch => BuildBranch(branch, defs, isRoot),
            SemanticLeafNode leaf => BuildLeaf(leaf),
            _ => throw new InternalDataProcessingException()
        };
    }

    private JsonSchemaBuilder BuildBranch(SemanticBranchNode branch, Dictionary<string, JsonSchemaBuilder> defs, bool isRoot = false)
    {
        if (!isRoot && defs.ContainsKey(branch.SemanticId))
        {
            return CreateRefBuilder(branch.SemanticId);
        }

        var requiredProperties = new List<string>();
        var children = BuildChildren(branch.Children, defs, requiredProperties);

        var schemaBuilder = IsArrayCardinality(branch.Cardinality)
            ? BuildArraySchema(children, requiredProperties)
            : new JsonSchemaBuilder()
                .Type(SchemaValueType.Object)
                .Properties(children)
                .Required(requiredProperties);

        if (isRoot)
        {
            return schemaBuilder;
        }

        defs[branch.SemanticId] = schemaBuilder;
        return CreateRefBuilder(branch.SemanticId);
    }

    private Dictionary<string, JsonSchemaBuilder> BuildChildren(
        ReadOnlyCollection<SemanticTreeNode> children,
        Dictionary<string, JsonSchemaBuilder> defs,
        List<string> required)
    {
        var properties = new Dictionary<string, JsonSchemaBuilder>();

        foreach (var child in children)
        {
            var childBuilder = child switch
            {
                SemanticBranchNode branch => BuildNode(branch, defs),
                SemanticLeafNode leaf => BuildLeaf(leaf),
                _ => throw new InternalDataProcessingException()
            };

            properties[child.SemanticId] = childBuilder;

            if (IsRequiredCardinality(child.Cardinality))
            {
                required.Add(child.SemanticId);
            }
        }

        return properties;
    }

    private static JsonSchemaBuilder BuildLeaf(SemanticLeafNode leaf)
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

        return new JsonSchemaBuilder().Type(type);
    }

    private JsonSchemaBuilder CreateRefBuilder(string semanticId)
        => new JsonSchemaBuilder().Ref($"{RefPrefix}{semanticId}");

    private static bool IsArrayCardinality(Cardinality cardinality)
        => cardinality is Cardinality.ZeroToMany or Cardinality.OneToMany;

    private static bool IsRequiredCardinality(Cardinality cardinality)
        => cardinality is Cardinality.One or Cardinality.OneToMany;
}

using System.Collections.ObjectModel;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using Json.Schema;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Helper;

public static class JsonSchemaGenerator
{
    private const string DefinitionsRefPrefix = "#/$defs/";

    public static JsonSchema ConvertToJsonSchema(SemanticTreeNode rootNode)
    {
        var branchUsage = CountBranchUsages(rootNode);
        var definitions = new Dictionary<string, JsonSchemaBuilder>();
        var rootSchema = BuildNode(rootNode, definitions, branchUsage, isRoot: true);

        return new JsonSchemaBuilder()
            .Schema(MetaSchemas.Draft202012Id)
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                [rootNode!.SemanticId] = rootSchema
            })
            .Defs(definitions)
            .Build();
    }

    private static JsonSchemaBuilder BuildNode(
        SemanticTreeNode node,
        Dictionary<string, JsonSchemaBuilder> definitions,
        Dictionary<string, int> branchUsage,
        bool isRoot = false)
    {
        return node switch
        {
            SemanticBranchNode branch => BuildBranch(branch, definitions, branchUsage, isRoot),
            SemanticLeafNode leaf => BuildLeaf(leaf),
            _ => throw new InternalDataProcessingException()
        };
    }

    private static JsonSchemaBuilder BuildBranch(
        SemanticBranchNode branch,
        Dictionary<string, JsonSchemaBuilder> definitions,
        Dictionary<string, int> branchUsage,
        bool isRoot = false)
    {
        if (!isRoot && ShouldUseReference(branch, branchUsage) && definitions.ContainsKey(branch.SemanticId))
        {
            return CreateRefSchema(branch.SemanticId);
        }

        var requiredProperties = new List<string>();
        var children = BuildChildren(branch.Children, definitions, branchUsage, requiredProperties);

        var schemaBuilder = IsArrayCardinality(branch.Cardinality)
            ? BuildArraySchema(children, requiredProperties)
            : BuildObjectSchema(children, requiredProperties);

        if (isRoot)
        {
            return schemaBuilder;
        }

        if (!ShouldUseReference(branch, branchUsage))
        {
            return schemaBuilder;
        }

        definitions[branch.SemanticId] = schemaBuilder;

        return CreateRefSchema(branch.SemanticId);
    }

    private static JsonSchemaBuilder BuildObjectSchema(
        Dictionary<string, JsonSchemaBuilder> properties,
        List<string> requiredProperties)
    {
        var schemaBuilder = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(properties);

        if (requiredProperties.Count > 0)
        {
            schemaBuilder = schemaBuilder.Required(requiredProperties);
        }

        return schemaBuilder;
    }

    private static JsonSchemaBuilder BuildArraySchema(
        Dictionary<string, JsonSchemaBuilder> properties,
        List<string> requiredProperties)
    {
        var itemBuilder = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(properties);

        if (requiredProperties.Count > 0)
        {
            itemBuilder = itemBuilder.Required(requiredProperties);
        }

        return new JsonSchemaBuilder()
            .Type(SchemaValueType.Array)
            .Items(itemBuilder);
    }

    private static Dictionary<string, JsonSchemaBuilder> BuildChildren(
        ReadOnlyCollection<SemanticTreeNode> children,
        Dictionary<string, JsonSchemaBuilder> definitions,
        Dictionary<string, int> branchUsage,
        List<string> required)
    {
        var properties = new Dictionary<string, JsonSchemaBuilder>();

        foreach (var child in children)
        {
            var childSchema = child switch
            {
                SemanticBranchNode branch => BuildNode(branch, definitions, branchUsage),
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

        var primitiveSchema = new JsonSchemaBuilder().Type(type);

        return IsArrayCardinality(leaf.Cardinality)
            ? new JsonSchemaBuilder().Type(SchemaValueType.Array).Items(primitiveSchema)
            : primitiveSchema;
    }

    private static Dictionary<string, int> CountBranchUsages(SemanticTreeNode rootNode)
    {
        var usage = new Dictionary<string, int>(StringComparer.Ordinal);
        CountBranchUsages(rootNode, usage);
        return usage;
    }

    private static void CountBranchUsages(SemanticTreeNode node, Dictionary<string, int> usage)
    {
        if (node is not SemanticBranchNode branch)
        {
            return;
        }

        usage.TryGetValue(branch.SemanticId, out var count);
        usage[branch.SemanticId] = count + 1;

        foreach (var child in branch.Children)
        {
            CountBranchUsages(child, usage);
        }
    }

    private static bool ShouldUseReference(SemanticBranchNode branch, Dictionary<string, int> branchUsage)
        => branchUsage.TryGetValue(branch.SemanticId, out var usageCount) && usageCount > 1;

    private static bool IsArrayCardinality(Cardinality cardinality)
        => cardinality is Cardinality.ZeroToMany or Cardinality.OneToMany;

    private static bool IsRequiredCardinality(Cardinality cardinality)
        => cardinality is Cardinality.One or Cardinality.OneToMany;

    private static JsonSchemaBuilder CreateRefSchema(string semanticId)
        => new JsonSchemaBuilder().Ref($"{DefinitionsRefPrefix}{semanticId}");
}

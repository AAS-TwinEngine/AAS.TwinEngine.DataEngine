using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Helper;

using Json.Schema;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Providers.PluginDataProvider.Helper;

public class JsonSchemaGeneratorTests
{
    [Fact]
    public void ConvertToJsonSchema_LeafNode_ReturnSchema()
    {
        const string SemanticId = "http://example.com/idta/digital-nameplate/contact-list/Name";
        var leaf = new SemanticLeafNode(
            semanticId: "http://example.com/idta/digital-nameplate/contact-list/Name",
            value: "",
            dataType: DataType.String,
            cardinality: Cardinality.One
        );

        var result = JsonSchemaGenerator.ConvertToJsonSchema(leaf);

        var typeKeyword = result.Keywords!.OfType<TypeKeyword>().SingleOrDefault();
        Assert.Equal(SchemaValueType.Object, typeKeyword!.Type);
        var propKeyword = result.Keywords!.OfType<PropertiesKeyword>().SingleOrDefault();
        Assert.True(propKeyword!.Properties.ContainsKey(SemanticId));
        var leafSchema = propKeyword.Properties[SemanticId];
        var leafTypeKeyword = leafSchema.Keywords!.OfType<TypeKeyword>().SingleOrDefault();
        Assert.NotNull(leafTypeKeyword);
        Assert.Equal(SchemaValueType.String, leafTypeKeyword.Type);
    }

    [Fact]
    public void ConvertToJsonSchema_OptionalLeafNode_IsNotRequired()
    {
        const string SemanticId = "http://example.com/optional";
        var leaf = new SemanticLeafNode(SemanticId, "", DataType.String, Cardinality.ZeroToOne);

        var schema = JsonSchemaGenerator.ConvertToJsonSchema(leaf);

        var propKeyword = schema.Keywords!.OfType<PropertiesKeyword>().SingleOrDefault();
        Assert.NotNull(propKeyword);
        Assert.True(propKeyword.Properties.ContainsKey(SemanticId));

        var requiredKeyword = schema.Keywords!.OfType<RequiredKeyword>().SingleOrDefault();
        Assert.Null(requiredKeyword);
    }

    [Fact]
    public void ConvertToJsonSchema_BranchNodeWithOneCardinality_ReturnsObjectSchema()
    {
        const string SemanticId = "http://example.com/idta/digital-nameplate/contact-list";
        const string NameId = "http://example.com/idta/digital-nameplate/contact-list/Name";
        const string WeightId = "http://example.com/idta/digital-nameplate/contact-list/Weight";
        var branch = new SemanticBranchNode(
            semanticId: "http://example.com/idta/digital-nameplate/contact-list",
            cardinality: Cardinality.One
        );
        branch.AddChild(new SemanticLeafNode(
            "http://example.com/idta/digital-nameplate/contact-list/Name", "", DataType.String, Cardinality.One));
        branch.AddChild(new SemanticLeafNode(
            "http://example.com/idta/digital-nameplate/contact-list/Weight", null!, DataType.Integer, Cardinality.ZeroToOne));

        var result = JsonSchemaGenerator.ConvertToJsonSchema(branch);

        var typeKeyword = result.Keywords!.OfType<TypeKeyword>().SingleOrDefault();
        Assert.Equal(SchemaValueType.Object, typeKeyword!.Type);
        var propKeyword = result.Keywords!.OfType<PropertiesKeyword>().SingleOrDefault();
        Assert.True(propKeyword!.Properties.ContainsKey(SemanticId));
        var branchSchema = propKeyword.Properties[SemanticId];
        var branchType = branchSchema.Keywords!.OfType<TypeKeyword>().SingleOrDefault();
        Assert.Equal(SchemaValueType.Object, branchType!.Type);

        var branchProps = branchSchema.Keywords!.OfType<PropertiesKeyword>().SingleOrDefault();
        Assert.NotNull(branchProps);
        Assert.True(branchProps.Properties.ContainsKey(NameId));
        Assert.True(branchProps.Properties.ContainsKey(WeightId));

        var nameSchema = branchProps.Properties[NameId];
        var nameType = nameSchema.Keywords!.OfType<TypeKeyword>().SingleOrDefault();
        Assert.NotNull(nameType);
        Assert.Equal(SchemaValueType.String, nameType.Type);

        var weightSchema = branchProps.Properties[WeightId];
        var weightType = weightSchema.Keywords!.OfType<TypeKeyword>().SingleOrDefault();
        Assert.NotNull(weightType);
        Assert.Equal(SchemaValueType.Integer, weightType.Type);

        var requiredKeyword = branchSchema.Keywords!.OfType<RequiredKeyword>().SingleOrDefault();
        Assert.NotNull(requiredKeyword);
        Assert.Contains(NameId, requiredKeyword.Properties);
        Assert.DoesNotContain(WeightId, requiredKeyword.Properties);
    }

    [Fact]
    public void ConvertToJsonSchema_BranchNodeWithZeroToManyCardinality_ReturnsArraySchema()
    {
        const string SemanticId = "http://example.com/idta/digital-nameplate/contact-list";
        const string NameId = "http://example.com/idta/digital-nameplate/contact-list/Name";
        var branchNode = new SemanticBranchNode("http://example.com/idta/digital-nameplate/contact-list", Cardinality.ZeroToMany);
        branchNode.AddChild(new SemanticLeafNode(
            "http://example.com/idta/digital-nameplate/contact-list/Name", "", DataType.String, Cardinality.One));

        var result = JsonSchemaGenerator.ConvertToJsonSchema(branchNode);

        var typeKeyword = result.Keywords!.OfType<TypeKeyword>().SingleOrDefault();
        Assert.Equal(SchemaValueType.Object, typeKeyword!.Type);

        var propKeyword = result.Keywords!.OfType<PropertiesKeyword>().SingleOrDefault();
        Assert.True(propKeyword!.Properties.ContainsKey(SemanticId));

        var arraySchema = propKeyword.Properties[SemanticId];
        var arrayType = arraySchema.Keywords!.OfType<TypeKeyword>().SingleOrDefault();
        Assert.Equal(SchemaValueType.Array, arrayType!.Type);

        var arraySchemaProps = arraySchema.Keywords!.OfType<PropertiesKeyword>().SingleOrDefault();
        Assert.True(arraySchemaProps!.Properties.ContainsKey(NameId));
        var nameSchema = arraySchemaProps.Properties[NameId];
        var nameType = nameSchema.Keywords!.OfType<TypeKeyword>().SingleOrDefault();
        Assert.NotNull(nameType);
        Assert.Equal(SchemaValueType.String, nameType.Type);

        var arrayRequiredProps = arraySchema.Keywords!.OfType<RequiredKeyword>().SingleOrDefault();
        var contains = arrayRequiredProps!.Properties.Contains(NameId);

        Assert.True(contains);
    }

    [Fact]
    public void ConvertToJsonSchema_NestedBranchNodes_UsesRefAndDefinitionsCorrectly()
    {
        const string RootSemanticId = "http://example.com/idta/digital-nameplate";
        const string BranchSemanticId = "http://example.com/idta/digital-nameplate/contact-list";
        const string NameId = "http://example.com/idta/digital-nameplate/contact-list/Name";
        var root = new SemanticBranchNode("http://example.com/idta/digital-nameplate", Cardinality.Unknown);
        var childBranch = new SemanticBranchNode("http://example.com/idta/digital-nameplate/contact-list", Cardinality.One);
        childBranch.AddChild(new SemanticLeafNode("http://example.com/idta/digital-nameplate/contact-list/Name", "", DataType.String, Cardinality.One));
        root.AddChild(childBranch);

        var schema = JsonSchemaGenerator.ConvertToJsonSchema(root);

        var typeKeyword = schema.Keywords!.OfType<TypeKeyword>().SingleOrDefault();
        Assert.NotNull(typeKeyword);
        Assert.Equal(SchemaValueType.Object, typeKeyword.Type);

        var propKeyword = schema.Keywords!.OfType<PropertiesKeyword>().SingleOrDefault();
        Assert.NotNull(propKeyword);
        Assert.True(propKeyword.Properties.ContainsKey(RootSemanticId));

        var rootPropSchema = propKeyword.Properties[RootSemanticId];
        var rootPropType = rootPropSchema.Keywords!.OfType<TypeKeyword>().SingleOrDefault();
        Assert.NotNull(rootPropType);
        Assert.Equal(SchemaValueType.Object, rootPropType.Type);

        var rootProps = rootPropSchema.Keywords!.OfType<PropertiesKeyword>().SingleOrDefault();
        Assert.NotNull(rootProps);
        Assert.True(rootProps.Properties.ContainsKey(BranchSemanticId));

        var branchRefSchema = rootProps.Properties[BranchSemanticId];
        var refKeyword = branchRefSchema.Keywords!.OfType<RefKeyword>().SingleOrDefault();
        Assert.NotNull(refKeyword);
        Assert.Equal($"#/definitions/{BranchSemanticId}", refKeyword.Reference.ToString());

        var definitionsKeyword = schema.Keywords!.OfType<DefinitionsKeyword>().SingleOrDefault();
        Assert.NotNull(definitionsKeyword);
        Assert.True(definitionsKeyword.Definitions.ContainsKey(BranchSemanticId));

        var branchDefSchema = definitionsKeyword.Definitions[BranchSemanticId];
        var branchType = branchDefSchema.Keywords!.OfType<TypeKeyword>().SingleOrDefault();
        Assert.NotNull(branchType);
        Assert.Equal(SchemaValueType.Object, branchType.Type);

        var branchProps = branchDefSchema.Keywords!.OfType<PropertiesKeyword>().SingleOrDefault();
        Assert.NotNull(branchProps);
        Assert.True(branchProps.Properties.ContainsKey(NameId));

        var nameSchema = branchProps.Properties[NameId];
        var nameType = nameSchema.Keywords!.OfType<TypeKeyword>().SingleOrDefault();
        Assert.NotNull(nameType);
        Assert.Equal(SchemaValueType.String, nameType.Type);

        var requiredKeyword = branchDefSchema.Keywords!.OfType<RequiredKeyword>().SingleOrDefault();
        Assert.NotNull(requiredKeyword);
        Assert.Contains(NameId, requiredKeyword.Properties);
    }

    [Fact]
    public void ConvertToJsonSchema_DataTypeMapping_ConvertsCorrectly()
    {
        var branch = new SemanticBranchNode("http://example.com/schema/data-types", Cardinality.One);
        branch.AddChild(new SemanticLeafNode("string", "", DataType.String, Cardinality.One));
        branch.AddChild(new SemanticLeafNode("integer", null!, DataType.Integer, Cardinality.One));
        branch.AddChild(new SemanticLeafNode("number", null!, DataType.Number, Cardinality.One));
        branch.AddChild(new SemanticLeafNode("boolean", null!, DataType.Boolean, Cardinality.One));
        branch.AddChild(new SemanticLeafNode("unknown", null!, DataType.Unknown, Cardinality.One));

        var schema = JsonSchemaGenerator.ConvertToJsonSchema(branch);

        var typeKeyword = schema.Keywords!.OfType<TypeKeyword>().SingleOrDefault();
        Assert.NotNull(typeKeyword);
        Assert.Equal(SchemaValueType.Object, typeKeyword.Type);

        var propKeyword = schema.Keywords!.OfType<PropertiesKeyword>().SingleOrDefault();
        Assert.NotNull(propKeyword);
        Assert.True(propKeyword.Properties.ContainsKey("http://example.com/schema/data-types"));

        var rootPropSchema = propKeyword.Properties["http://example.com/schema/data-types"];
        var rootType = rootPropSchema.Keywords!.OfType<TypeKeyword>().SingleOrDefault();
        Assert.NotNull(rootType);
        Assert.Equal(SchemaValueType.Object, rootType.Type);
        var rootProps = rootPropSchema.Keywords!.OfType<PropertiesKeyword>().SingleOrDefault();
        Assert.NotNull(rootProps);
        Assert.Equal(SchemaValueType.String, GetTypeForProperty(rootProps, "string"));
        Assert.Equal(SchemaValueType.Integer, GetTypeForProperty(rootProps, "integer"));
        Assert.Equal(SchemaValueType.Number, GetTypeForProperty(rootProps, "number"));
        Assert.Equal(SchemaValueType.Boolean, GetTypeForProperty(rootProps, "boolean"));
        Assert.Equal(SchemaValueType.String, GetTypeForProperty(rootProps, "unknown"));
    }

    [Fact]
    public void ConvertToJsonSchema_UnsupportedNode_ThrowsException()
    {
        var unsupportedNode = new UnsupportedSemanticNode("unsupported", Cardinality.One);

        var ex = Assert.Throws<InternalDataProcessingException>(() =>
                                                                    JsonSchemaGenerator.ConvertToJsonSchema(unsupportedNode));
    }

    private sealed class UnsupportedSemanticNode(string semanticId, Cardinality cardinality) : SemanticTreeNode(semanticId, cardinality);

    private static SchemaValueType GetTypeForProperty(PropertiesKeyword props, string key)
    {
        var schema = props.Properties[key];
        var typeKeyword = schema.Keywords!.OfType<TypeKeyword>().SingleOrDefault();
        Assert.NotNull(typeKeyword);
        return typeKeyword.Type;
    }
}

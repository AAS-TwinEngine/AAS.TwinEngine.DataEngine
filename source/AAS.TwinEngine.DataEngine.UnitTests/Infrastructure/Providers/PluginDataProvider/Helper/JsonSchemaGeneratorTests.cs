using System.Text.Json;
using System.Text.Json.Nodes;

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
        var leaf = new SemanticLeafNode(SemanticId, "", DataType.String, Cardinality.One);

        var result = JsonSchemaGenerator.ConvertToJsonSchema(leaf);

        var json = ToJson(result);
        Assert.Equal("object", json["type"]!.ToString());
        var props = json["properties"]!;
        Assert.NotNull(props[SemanticId]);
        var leafSchema = props[SemanticId];
        Assert.Equal("string", leafSchema!["type"]!.ToString());
    }

    [Fact]
    public void ConvertToJsonSchema_OptionalLeafNode_IsNotRequired()
    {
        const string SemanticId = "http://example.com/optional";

        var leaf = new SemanticLeafNode(SemanticId, "", DataType.String, Cardinality.ZeroToOne);

        var schema = JsonSchemaGenerator.ConvertToJsonSchema(leaf);
        var json = ToJson(schema);

        var props = json["properties"]!;
        Assert.NotNull(props[SemanticId]);
        Assert.Null(json["required"]);
    }

    [Fact]
    public void ConvertToJsonSchema_BranchNodeWithOneCardinality_ReturnsObjectSchema()
    {
        const string SemanticId = "http://example.com/idta/digital-nameplate/contact-list";
        const string NameId = "http://example.com/idta/digital-nameplate/contact-list/Name";
        const string WeightId = "http://example.com/idta/digital-nameplate/contact-list/Weight";
        var branch = new SemanticBranchNode(SemanticId, Cardinality.One);
        branch.AddChild(new SemanticLeafNode(NameId, "", DataType.String, Cardinality.One));
        branch.AddChild(new SemanticLeafNode(WeightId, null!, DataType.Integer, Cardinality.ZeroToOne));

        var result = JsonSchemaGenerator.ConvertToJsonSchema(branch);

        var json = ToJson(result);
        Assert.Equal("object", json["type"]!.ToString());
        var rootProps = json["properties"]!;
        var branchSchema = rootProps[SemanticId]!;
        Assert.Equal("object", branchSchema["type"]!.ToString());
        var branchProps = branchSchema["properties"]!;
        Assert.NotNull(branchProps[NameId]);
        Assert.NotNull(branchProps[WeightId]);
        Assert.Equal("string", branchProps[NameId]!["type"]!.ToString());
        Assert.Equal("integer", branchProps[WeightId]!["type"]!.ToString());
        var required = branchSchema["required"]!.AsArray();
        Assert.Contains(NameId, required.Select(x => x!.ToString()));
        Assert.DoesNotContain(WeightId, required.Select(x => x!.ToString()));
    }

    [Fact]
    public void ConvertToJsonSchema_BranchNodeWithZeroToManyCardinality_ReturnsArraySchema()
    {
        const string SemanticId = "http://example.com/idta/digital-nameplate/contact-list";
        const string NameId = "http://example.com/idta/digital-nameplate/contact-list/Name";
        var branchNode = new SemanticBranchNode(SemanticId, Cardinality.ZeroToMany);
        branchNode.AddChild(new SemanticLeafNode(NameId, "", DataType.String, Cardinality.One));

        var result = JsonSchemaGenerator.ConvertToJsonSchema(branchNode);

        var json = ToJson(result);
        Assert.Equal("object", json["type"]!.ToString());
        var rootProps = json["properties"]!;
        var arraySchema = rootProps[SemanticId]!;
        Assert.Equal("array", arraySchema["type"]!.ToString());
        var items = arraySchema["items"]!;
        var props = items["properties"]!;
        Assert.True(props[NameId] != null);
        Assert.Equal("string", props[NameId]!["type"]!.ToString());
        var required = items["required"]!.AsArray();
        Assert.Contains(NameId, required.Select(x => x!.ToString()));
    }

    [Fact]
    public void ConvertToJsonSchema_NestedBranchNodes_UsesInlineSchemaWithoutDefs()
    {
        const string RootSemanticId = "http://example.com/idta/digital-nameplate";
        const string BranchSemanticId = "http://example.com/idta/digital-nameplate/contact-list";
        const string NameId = "http://example.com/idta/digital-nameplate/contact-list/Name";
        var root = new SemanticBranchNode(RootSemanticId, Cardinality.Unknown);
        var childBranch = new SemanticBranchNode(BranchSemanticId, Cardinality.One);
        childBranch.AddChild(new SemanticLeafNode(NameId, "", DataType.String, Cardinality.One));
        root.AddChild(childBranch);

        var schema = JsonSchemaGenerator.ConvertToJsonSchema(root);
        var json = ToJson(schema);

        Assert.Equal("object", json["type"]!.ToString());
        var rootProps = json["properties"]![RootSemanticId]!["properties"]!;
        var branchSchema = rootProps[BranchSemanticId]!;
        Assert.Null(branchSchema["$ref"]);
        Assert.Equal("object", branchSchema["type"]!.ToString());
        var defs = json["$defs"];
        Assert.True(defs == null || defs.AsObject().Count == 0);
        var branchProps = branchSchema["properties"]!;
        Assert.NotNull(branchProps[NameId]);
        var required = branchSchema["required"]!.AsArray();
        Assert.Contains(NameId, required.Select(x => x!.ToString()));
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
        var json = ToJson(schema);

        var rootProps = json["properties"]!["http://example.com/schema/data-types"]!["properties"]!;

        Assert.Equal("string", rootProps["string"]!["type"]!.ToString());
        Assert.Equal("integer", rootProps["integer"]!["type"]!.ToString());
        Assert.Equal("number", rootProps["number"]!["type"]!.ToString());
        Assert.Equal("boolean", rootProps["boolean"]!["type"]!.ToString());
        Assert.Equal("string", rootProps["unknown"]!["type"]!.ToString());
    }

    [Fact]
    public void ConvertToJsonSchema_ArraySchema_UsesItemsKeyword()
    {
        var branch = new SemanticBranchNode("root", Cardinality.ZeroToMany);
        branch.AddChild(new SemanticLeafNode("child", "", DataType.String, Cardinality.One));

        var schema = JsonSchemaGenerator.ConvertToJsonSchema(branch);
        var json = ToJson(schema);

        var rootProps = json["properties"]!["root"]!;

        Assert.Equal("array", rootProps["type"]!.ToString());
        Assert.NotNull(rootProps["items"]);
    }

    [Fact]
    public void ConvertToJsonSchema_LeafWithOneToManyCardinality_UsesArrayItems()
    {
        var branch = new SemanticBranchNode("root", Cardinality.One);
        branch.AddChild(new SemanticLeafNode("tags", "", DataType.String, Cardinality.OneToMany));

        var schema = JsonSchemaGenerator.ConvertToJsonSchema(branch);
        var json = ToJson(schema);

        var leaf = json["properties"]!["root"]!["properties"]!["tags"]!;
        Assert.Equal("array", leaf["type"]!.ToString());
        Assert.Equal("string", leaf["items"]!["type"]!.ToString());
    }

    [Fact]
    public void ConvertToJsonSchema_UnsupportedNode_ThrowsException()
    {
        var unsupportedNode = new UnsupportedSemanticNode("unsupported", Cardinality.One);

        Assert.Throws<InternalDataProcessingException>(() =>
            JsonSchemaGenerator.ConvertToJsonSchema(unsupportedNode));
    }

    [Fact]
    public void ConvertToJsonSchema_EmptyBranchNode_ReturnsEmptyObjectSchema()
    {
        var branch = new SemanticBranchNode("root", Cardinality.One);

        var schema = JsonSchemaGenerator.ConvertToJsonSchema(branch);
        var json = ToJson(schema);

        var rootProps = json["properties"]!["root"]!;
        Assert.Equal("object", rootProps["type"]!.ToString());
        Assert.NotNull(rootProps["properties"]);
        Assert.Empty(rootProps["properties"]!.AsObject());
    }

    [Fact]
    public void ConvertToJsonSchema_ArrayWithOptionalChildren_DoesNotContainRequired()
    {
        var branch = new SemanticBranchNode("root", Cardinality.ZeroToMany);
        branch.AddChild(new SemanticLeafNode("child", "", DataType.String, Cardinality.ZeroToOne));

        var schema = JsonSchemaGenerator.ConvertToJsonSchema(branch);
        var json = ToJson(schema);

        var items = json["properties"]!["root"]!["items"]!;
        Assert.Null(items["required"]);
    }

    [Fact]
    public void ConvertToJsonSchema_ReusedBranch_UsesSingleDefinition()
    {
        var shared = new SemanticBranchNode("shared", Cardinality.One);
        shared.AddChild(new SemanticLeafNode("name", "", DataType.String, Cardinality.One));
        var root = new SemanticBranchNode("root", Cardinality.One);
        root.AddChild(shared);
        root.AddChild(shared);

        var schema = JsonSchemaGenerator.ConvertToJsonSchema(root);

        var json = ToJson(schema);
        var defs = json["$defs"]!;
        Assert.Single(defs.AsObject());
        var rootProps = json["properties"]!["root"]!["properties"]!;
        Assert.Equal("#/$defs/shared", rootProps["shared"]!["$ref"]!.ToString());
    }

    [Fact]
    public void ConvertToJsonSchema_DeepNestedStructure_WorksCorrectly()
    {
        var level3 = new SemanticBranchNode("level3", Cardinality.One);
        level3.AddChild(new SemanticLeafNode("leaf", "", DataType.String, Cardinality.One));
        var level2 = new SemanticBranchNode("level2", Cardinality.One);
        level2.AddChild(level3);
        var level1 = new SemanticBranchNode("level1", Cardinality.One);
        level1.AddChild(level2);

        var schema = JsonSchemaGenerator.ConvertToJsonSchema(level1);

        var json = ToJson(schema);
        var level2Schema = json["properties"]!["level1"]!["properties"]!["level2"]!;
        Assert.Equal("object", level2Schema["type"]!.ToString());
        var level3Schema = level2Schema["properties"]!["level3"]!;
        Assert.Equal("object", level3Schema["type"]!.ToString());
        var leafSchema = level3Schema["properties"]!["leaf"]!;
        Assert.Equal("string", leafSchema["type"]!.ToString());
    }

    [Fact]
    public void ConvertToJsonSchema_MixedCardinality_WorksCorrectly()
    {
        var root = new SemanticBranchNode("root", Cardinality.One);
        var objectChild = new SemanticBranchNode("objectChild", Cardinality.One);
        objectChild.AddChild(new SemanticLeafNode("name", "", DataType.String, Cardinality.One));
        var arrayChild = new SemanticBranchNode("arrayChild", Cardinality.ZeroToMany);
        arrayChild.AddChild(new SemanticLeafNode("value", "", DataType.Number, Cardinality.One));
        root.AddChild(objectChild);
        root.AddChild(arrayChild);

        var schema = JsonSchemaGenerator.ConvertToJsonSchema(root);

        var json = ToJson(schema);
        var props = json["properties"]!["root"]!["properties"]!;
        var objectSchema = props["objectChild"]!;
        Assert.Equal("object", objectSchema["type"]!.ToString());
        var arraySchema = props["arrayChild"]!;
        Assert.Equal("array", arraySchema["type"]!.ToString());
        Assert.NotNull(arraySchema["items"]);
        var defs = json["$defs"];
        Assert.True(defs == null || defs.AsObject().Count == 0);
    }

    [Fact]
    public void ConvertToJsonSchema_SingleNestedBranch_OmitsRefAndDefs()
    {
        var root = new SemanticBranchNode("root", Cardinality.One);
        var child = new SemanticBranchNode("child", Cardinality.One);
        child.AddChild(new SemanticLeafNode("name", "", DataType.String, Cardinality.One));
        root.AddChild(child);

        var schema = JsonSchemaGenerator.ConvertToJsonSchema(root);
        var json = ToJson(schema);

        var childSchema = json["properties"]!["root"]!["properties"]!["child"]!;
        Assert.Null(childSchema["$ref"]);
        Assert.Equal("object", childSchema["type"]!.ToString());
        Assert.True(json["$defs"] == null || json["$defs"]!.AsObject().Count == 0);
    }

    [Fact]
    public void ConvertToJsonSchema_UnknownCardinality_TreatedAsObject()
    {
        var branch = new SemanticBranchNode("root", Cardinality.Unknown);
        branch.AddChild(new SemanticLeafNode("name", "", DataType.String, Cardinality.One));

        var schema = JsonSchemaGenerator.ConvertToJsonSchema(branch);

        var json = ToJson(schema);
        var root = json["properties"]!["root"]!;
        Assert.Equal("object", root["type"]!.ToString());
    }

    [Fact]
    public void ConvertToJsonSchema_MultipleRequiredFields_AllIncluded()
    {
        var branch = new SemanticBranchNode("root", Cardinality.One);
        branch.AddChild(new SemanticLeafNode("a", "", DataType.String, Cardinality.One));
        branch.AddChild(new SemanticLeafNode("b", "", DataType.String, Cardinality.One));

        var schema = JsonSchemaGenerator.ConvertToJsonSchema(branch);

        var json = ToJson(schema);
        var required = json["properties"]!["root"]!["required"]!.AsArray();
        Assert.Contains("a", required.Select(x => x!.ToString()));
        Assert.Contains("b", required.Select(x => x!.ToString()));
    }

    private sealed class UnsupportedSemanticNode(string semanticId, Cardinality cardinality)
        : SemanticTreeNode(semanticId, cardinality);

    private static readonly JsonSerializerOptions SerializerOption = new()
    {
        WriteIndented = false
    };

    private static JsonNode ToJson(JsonSchema schema)
        => JsonSerializer.SerializeToNode(schema, SerializerOption)!;
}

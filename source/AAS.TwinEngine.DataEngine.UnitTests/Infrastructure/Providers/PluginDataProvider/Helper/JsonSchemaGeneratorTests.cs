using System.Text.Json;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Helper;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Helper;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Helper.LegacyV1;

using Json.Schema;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Providers.PluginDataProvider.Helper;

public class JsonSchemaDraft202012GeneratorTests
{
    private readonly IJsonSchemaGenerator _sut = new JsonSchemaDraft202012Generator();

    [Fact]
    public void Generate_WhenLeafNode_ReturnsDraft202012Schema()
    {
        const string SemanticId = "http://example.com/idta/digital-nameplate/contact-list/Name";
        var leaf = new SemanticLeafNode(SemanticId, "", DataType.String, Cardinality.One);

        var schema = _sut.Generate(leaf);

        var el = ToElement(schema);
        Assert.Equal("https://json-schema.org/draft/2020-12/schema", el.GetProperty("$schema").GetString());
        Assert.Equal("object", el.GetProperty("type").GetString());
        Assert.True(el.GetProperty("properties").TryGetProperty(SemanticId, out var leafEl));
        Assert.Equal("string", leafEl.GetProperty("type").GetString());
    }

    [Fact]
    public void Generate_WhenNestedBranches_UsesDollarDefsAndDraft202012Refs()
    {
        const string RootId = "http://example.com/idta/root";
        const string BranchId = "http://example.com/idta/branch";
        const string NameId = "http://example.com/idta/name";
        var root = new SemanticBranchNode(RootId, Cardinality.One);
        var branch = new SemanticBranchNode(BranchId, Cardinality.One);
        branch.AddChild(new SemanticLeafNode(NameId, "", DataType.String, Cardinality.One));
        root.AddChild(branch);

        var schema = _sut.Generate(root);

        var el = ToElement(schema);
        var branchRefEl = el.GetProperty("properties").GetProperty(RootId)
                            .GetProperty("properties").GetProperty(BranchId);
        Assert.Equal($"#/$defs/{BranchId}", branchRefEl.GetProperty("$ref").GetString());
        Assert.True(el.TryGetProperty("$defs", out var defsEl));
        Assert.False(el.TryGetProperty("definitions", out _));
        Assert.True(defsEl.TryGetProperty(BranchId, out var branchDef));
        Assert.Equal("object", branchDef.GetProperty("type").GetString());
        Assert.Equal("string", branchDef.GetProperty("properties").GetProperty(NameId).GetProperty("type").GetString());
    }

    [Fact]
    public void Generate_WhenBranchWithZeroToManyCardinality_WrapsChildrenInItems()
    {
        const string BranchId = "http://example.com/list";
        const string ChildId = "http://example.com/list/item";
        var branch = new SemanticBranchNode(BranchId, Cardinality.ZeroToMany);
        branch.AddChild(new SemanticLeafNode(ChildId, "", DataType.String, Cardinality.One));

        var schema = _sut.Generate(branch);

        var el = ToElement(schema);
        var branchEl = el.GetProperty("properties").GetProperty(BranchId);
        Assert.Equal("array", branchEl.GetProperty("type").GetString());
        Assert.True(branchEl.TryGetProperty("items", out var itemsEl));
        Assert.Equal("object", itemsEl.GetProperty("type").GetString());
        Assert.True(itemsEl.TryGetProperty("properties", out var itemPropsEl));
        Assert.True(itemPropsEl.TryGetProperty(ChildId, out _));
        Assert.False(branchEl.TryGetProperty("properties", out _));
    }

    [Fact]
    public void Generate_WhenLeafNodeIsOptional_DoesNotMarkPropertyAsRequired()
    {
        const string SemanticId = "http://example.com/optional";
        var leaf = new SemanticLeafNode(SemanticId, "", DataType.String, Cardinality.ZeroToOne);

        var schema = _sut.Generate(leaf);
        var el = ToElement(schema);

        Assert.True(el.GetProperty("properties").TryGetProperty(SemanticId, out _));
        Assert.False(el.TryGetProperty("required", out _));
    }

    [Fact]
    public void Generate_WhenBranchHasMixedCardinality_SetsRequiredOnlyForMandatoryChildren()
    {
        const string BranchId = "http://example.com/branch";
        const string RequiredChildId = "http://example.com/branch/required";
        const string OptionalChildId = "http://example.com/branch/optional";
        var branch = new SemanticBranchNode(BranchId, Cardinality.One);
        branch.AddChild(new SemanticLeafNode(RequiredChildId, "", DataType.String, Cardinality.One));
        branch.AddChild(new SemanticLeafNode(OptionalChildId, "", DataType.String, Cardinality.ZeroToOne));

        var schema = _sut.Generate(branch);

        var branchEl = ToElement(schema).GetProperty("properties").GetProperty(BranchId);
        var required = branchEl.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains(RequiredChildId, required);
        Assert.DoesNotContain(OptionalChildId, required);
    }

    [Theory]
    [InlineData(DataType.String, "string")]
    [InlineData(DataType.Integer, "integer")]
    [InlineData(DataType.Number, "number")]
    [InlineData(DataType.Boolean, "boolean")]
    [InlineData(DataType.Unknown, "string")]
    public void Generate_WhenLeafWithDataType_MapsToCorrectJsonType(DataType dataType, string expectedJsonType)
    {
        const string SemanticId = "http://example.com/leaf";
        var leaf = new SemanticLeafNode(SemanticId, null!, dataType, Cardinality.One);

        var schema = _sut.Generate(leaf);

        var leafEl = ToElement(schema).GetProperty("properties").GetProperty(SemanticId);
        Assert.Equal(expectedJsonType, leafEl.GetProperty("type").GetString());
    }

    [Fact]
    public void Generate_WhenUnsupportedNode_ThrowsInternalDataProcessingException()
    {
        var unsupportedNode = new UnsupportedSemanticNode("unsupported", Cardinality.One);

        Assert.Throws<InternalDataProcessingException>(() => _sut.Generate(unsupportedNode));
    }

    private static JsonElement ToElement(JsonSchema schema)
    {
        var json = JsonSerializer.Serialize(schema);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private sealed class UnsupportedSemanticNode(string semanticId, Cardinality cardinality) : SemanticTreeNode(semanticId, cardinality);
}

#pragma warning disable CS0618
public class LegacyDraft7JsonSchemaGeneratorTests
{
    private readonly IJsonSchemaGenerator _sut = new LegacyDraft7JsonSchemaGenerator();

    [Fact]
    public void Generate_WhenLeafNode_ReturnsDraft7Schema()
    {
        const string SemanticId = "http://example.com/idta/digital-nameplate/contact-list/Name";
        var leaf = new SemanticLeafNode(SemanticId, "", DataType.String, Cardinality.One);

        var schema = _sut.Generate(leaf);

        var el = ToElement(schema);
        Assert.Equal("http://json-schema.org/draft-07/schema#", el.GetProperty("$schema").GetString());
        Assert.Equal("object", el.GetProperty("type").GetString());
        Assert.True(el.GetProperty("properties").TryGetProperty(SemanticId, out var leafEl));
        Assert.Equal("string", leafEl.GetProperty("type").GetString());
    }

    [Fact]
    public void Generate_WhenNestedBranches_UsesDefinitionsAndDraft7Refs()
    {
        const string RootId = "http://example.com/idta/root";
        const string BranchId = "http://example.com/idta/branch";
        const string NameId = "http://example.com/idta/name";
        var root = new SemanticBranchNode(RootId, Cardinality.One);
        var branch = new SemanticBranchNode(BranchId, Cardinality.One);
        branch.AddChild(new SemanticLeafNode(NameId, "", DataType.String, Cardinality.One));
        root.AddChild(branch);

        var schema = _sut.Generate(root);

        var el = ToElement(schema);
        var branchRefEl = el.GetProperty("properties").GetProperty(RootId)
                            .GetProperty("properties").GetProperty(BranchId);
        Assert.Equal($"#/definitions/{BranchId}", branchRefEl.GetProperty("$ref").GetString());
        Assert.True(el.TryGetProperty("definitions", out var defsEl));
        Assert.False(el.TryGetProperty("$defs", out _));
        Assert.True(defsEl.TryGetProperty(BranchId, out var branchDef));
        Assert.Equal("object", branchDef.GetProperty("type").GetString());
        Assert.Equal("string", branchDef.GetProperty("properties").GetProperty(NameId).GetProperty("type").GetString());
    }

    [Fact]
    public void Generate_WhenBranchWithZeroToManyCardinality_ReturnsArrayWithDirectProperties()
    {
        const string BranchId = "http://example.com/list";
        const string ChildId = "http://example.com/list/item";
        var branch = new SemanticBranchNode(BranchId, Cardinality.ZeroToMany);
        branch.AddChild(new SemanticLeafNode(ChildId, "", DataType.String, Cardinality.One));

        var schema = _sut.Generate(branch);

        var el = ToElement(schema);
        var branchEl = el.GetProperty("properties").GetProperty(BranchId);
        Assert.Equal("array", branchEl.GetProperty("type").GetString());
        Assert.False(branchEl.TryGetProperty("items", out _));
        Assert.True(branchEl.TryGetProperty("properties", out var directPropsEl));
        Assert.True(directPropsEl.TryGetProperty(ChildId, out _));
    }

    [Fact]
    public void Generate_WhenLeafNodeIsOptional_DoesNotMarkPropertyAsRequired()
    {
        const string SemanticId = "http://example.com/optional";
        var leaf = new SemanticLeafNode(SemanticId, "", DataType.String, Cardinality.ZeroToOne);

        var schema = _sut.Generate(leaf);
        var el = ToElement(schema);

        Assert.True(el.GetProperty("properties").TryGetProperty(SemanticId, out _));
        Assert.False(el.TryGetProperty("required", out _));
    }

    [Fact]
    public void Generate_WhenBranchHasMixedCardinality_SetsRequiredOnlyForMandatoryChildren()
    {
        const string BranchId = "http://example.com/branch";
        const string RequiredChildId = "http://example.com/branch/required";
        const string OptionalChildId = "http://example.com/branch/optional";
        var branch = new SemanticBranchNode(BranchId, Cardinality.One);
        branch.AddChild(new SemanticLeafNode(RequiredChildId, "", DataType.String, Cardinality.One));
        branch.AddChild(new SemanticLeafNode(OptionalChildId, "", DataType.String, Cardinality.ZeroToOne));

        var schema = _sut.Generate(branch);

        var branchEl = ToElement(schema).GetProperty("properties").GetProperty(BranchId);
        var required = branchEl.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains(RequiredChildId, required);
        Assert.DoesNotContain(OptionalChildId, required);
    }

    [Theory]
    [InlineData(DataType.String, "string")]
    [InlineData(DataType.Integer, "integer")]
    [InlineData(DataType.Number, "number")]
    [InlineData(DataType.Boolean, "boolean")]
    [InlineData(DataType.Unknown, "string")]
    public void Generate_WhenLeafWithDataType_MapsToCorrectJsonType(DataType dataType, string expectedJsonType)
    {
        const string SemanticId = "http://example.com/leaf";
        var leaf = new SemanticLeafNode(SemanticId, null!, dataType, Cardinality.One);

        var schema = _sut.Generate(leaf);

        var leafEl = ToElement(schema).GetProperty("properties").GetProperty(SemanticId);
        Assert.Equal(expectedJsonType, leafEl.GetProperty("type").GetString());
    }

    [Fact]
    public void Generate_WhenUnsupportedNode_ThrowsInternalDataProcessingException()
    {
        var unsupportedNode = new UnsupportedSemanticNode("unsupported", Cardinality.One);

        Assert.Throws<InternalDataProcessingException>(() => _sut.Generate(unsupportedNode));
    }

    [Fact]
    public void Generate_WhenBranchContainsUnsupportedChild_ThrowsInternalDataProcessingException()
    {
        var root = new SemanticBranchNode("http://example.com/root", Cardinality.One);
        root.AddChild(new UnsupportedSemanticNode("http://example.com/unsupported", Cardinality.One));

        Assert.Throws<InternalDataProcessingException>(() => _sut.Generate(root));
    }

    [Fact]
    public void Generate_WhenNestedBranchesReuseSemanticId_CreatesSingleDefinitionAndSharedRefs()
    {
        const string RootId = "http://example.com/root";
        const string ParentOneId = "http://example.com/parent/one";
        const string ParentTwoId = "http://example.com/parent/two";
        const string SharedBranchId = "http://example.com/shared";
        const string LeafId = "http://example.com/shared/leaf";
        var root = new SemanticBranchNode(RootId, Cardinality.One);
        var parentOne = new SemanticBranchNode(ParentOneId, Cardinality.One);
        var parentTwo = new SemanticBranchNode(ParentTwoId, Cardinality.One);
        var sharedBranchTemplate = new SemanticBranchNode(SharedBranchId, Cardinality.One);
        sharedBranchTemplate.AddChild(new SemanticLeafNode(LeafId, string.Empty, DataType.String, Cardinality.One));

        parentOne.AddChild(sharedBranchTemplate);

        var sharedBranchReuse = new SemanticBranchNode(SharedBranchId, Cardinality.One);
        sharedBranchReuse.AddChild(new SemanticLeafNode(LeafId, string.Empty, DataType.String, Cardinality.One));
        parentTwo.AddChild(sharedBranchReuse);

        root.AddChild(parentOne);
        root.AddChild(parentTwo);

        var schema = _sut.Generate(root);

        var rootElement = ToElement(schema);
        var definitions = rootElement.GetProperty("definitions");
        var parentOneShared = definitions.GetProperty(ParentOneId).GetProperty("properties").GetProperty(SharedBranchId);
        var parentTwoShared = definitions.GetProperty(ParentTwoId).GetProperty("properties").GetProperty(SharedBranchId);

        Assert.Equal($"#/definitions/{SharedBranchId}", parentOneShared.GetProperty("$ref").GetString());
        Assert.Equal($"#/definitions/{SharedBranchId}", parentTwoShared.GetProperty("$ref").GetString());
        Assert.True(definitions.TryGetProperty(SharedBranchId, out var sharedDefinition));
        Assert.Equal("string", sharedDefinition.GetProperty("properties").GetProperty(LeafId).GetProperty("type").GetString());
    }

    private static JsonElement ToElement(JsonSchema schema)
    {
        var json = JsonSerializer.Serialize(schema);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private sealed class UnsupportedSemanticNode(string semanticId, Cardinality cardinality) : SemanticTreeNode(semanticId, cardinality);
}
#pragma warning restore CS0618

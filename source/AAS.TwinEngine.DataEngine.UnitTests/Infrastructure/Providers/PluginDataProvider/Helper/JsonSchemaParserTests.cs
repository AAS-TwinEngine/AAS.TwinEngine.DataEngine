using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Helper;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Providers.PluginDataProvider.Helper;

public class JsonSchemaParserTests
{
    [Fact]
    public void ParseJsonSchema_ShouldParseSimpleObject()
    {
        const string Json = """
                   {
                   "leaf1": "value1"
                   }
                   """;

        var result = JsonSchemaParser.ParseJsonSchema(Json);

        var leaf = Assert.IsType<SemanticLeafNode>(result);
        Assert.Equal("leaf1", leaf.SemanticId);
        Assert.Equal("value1", leaf.Value);
        Assert.Equal(DataType.Unknown, leaf.DataType);
        Assert.Equal(Cardinality.Unknown, leaf.Cardinality);
    }

    [Fact]
    public void ParseJsonSchema_ShouldParseNestedObject()
    {
        const string Json = """
                   {
                   "branch0": {
                   "leaf1": "value1",
                   "leaf2": "value2"
                   }
                   }
                   """;

        var result = JsonSchemaParser.ParseJsonSchema(Json);

        var branch = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal("branch0", branch.SemanticId);
        Assert.Equal(Cardinality.Unknown, branch.Cardinality);
        Assert.Equal(2, branch.Children.Count);
        var leaf1 = Assert.IsType<SemanticLeafNode>(branch.Children[0]);
        Assert.Equal("leaf1", leaf1.SemanticId);
        Assert.Equal("value1", leaf1.Value);
        Assert.Equal(DataType.Unknown, leaf1.DataType);
        Assert.Equal(Cardinality.Unknown, leaf1.Cardinality);
        var leaf2 = Assert.IsType<SemanticLeafNode>(branch.Children[1]);
        Assert.Equal("leaf2", leaf2.SemanticId);
        Assert.Equal("value2", leaf2.Value);
        Assert.Equal(DataType.Unknown, leaf2.DataType);
        Assert.Equal(Cardinality.Unknown, leaf2.Cardinality);
    }

    [Fact]
    public void ParseJsonSchema_ShouldParseArrayOfObjects()
    {
        const string Json = """
                   {
                   "items": [
                   { "name": "item1" },
                   { "name": "item2" }
                   ]
                   }
                   """;

        var result = JsonSchemaParser.ParseJsonSchema(Json);

        var itemsBranch = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal("items", itemsBranch.SemanticId);
        Assert.Equal(Cardinality.Unknown, itemsBranch.Cardinality);
        Assert.Equal(2, itemsBranch.Children.Count);
        var item1 = Assert.IsType<SemanticBranchNode>(itemsBranch.Children[0]);
        Assert.Equal("items", item1.SemanticId);
        Assert.Single(item1.Children);
        var name1 = Assert.IsType<SemanticLeafNode>(item1.Children[0]);
        Assert.Equal("name", name1.SemanticId);
        Assert.Equal("item1", name1.Value);
        var item2 = Assert.IsType<SemanticBranchNode>(itemsBranch.Children[1]);
        Assert.Equal("items", item2.SemanticId);
        Assert.Single(item2.Children);
        var name2 = Assert.IsType<SemanticLeafNode>(item2.Children[0]);
        Assert.Equal("name", name2.SemanticId);
        Assert.Equal("item2", name2.Value);
    }

    [Fact]
    public void ParseJsonSchema_ShouldParseJsonStringContainingJson()
    {
        const string Json = """
                   "{ \"leaf1\": \"value1\" }"
                   """;

        var result = JsonSchemaParser.ParseJsonSchema(Json);

        var leaf = Assert.IsType<SemanticLeafNode>(result);
        Assert.Equal("leaf1", leaf.SemanticId);
        Assert.Equal("value1", leaf.Value);
    }

    [Fact]
    public void ParseJsonSchema_ShouldHandleEmptyObject()
    {
        const string Json = "{}";

        var result = JsonSchemaParser.ParseJsonSchema(Json);

        var branch = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal(string.Empty, branch.SemanticId);
        Assert.Empty(branch.Children);
    }

    [Fact]
    public void ParseJsonSchema_ShouldHandleEmptyArray()
    {
        const string Json = """
                   {
                   "items": []
                   }
                   """;

        var result = JsonSchemaParser.ParseJsonSchema(Json);

        var itemsBranch = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal("items", itemsBranch.SemanticId);
        Assert.Empty(itemsBranch.Children);
    }
}

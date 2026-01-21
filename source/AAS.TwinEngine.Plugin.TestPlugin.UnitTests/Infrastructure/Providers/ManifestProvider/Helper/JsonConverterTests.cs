using System.Text.Json;

using AAS.TwinEngine.Plugin.TestPlugin.DomainModel.Submodel;
using AAS.TwinEngine.Plugin.TestPlugin.Infrastructure.Providers.ManifestProvider.Helper;

namespace AAS.TwinEngine.Plugin.TestPlugin.UnitTests.Infrastructure.Providers.ManifestProvider.Helper;

public class JsonConverterTests
{
    [Fact]
    public void ParseJson_ShouldReturnBranchNode_WhenJsonIsObject()
    {
        const string Json = """
                            {
                            "name": "value"
                            }
                            """;
        using var doc = JsonDocument.Parse(Json);

        var result = JsonConverter.ParseJson(doc);

        Assert.IsType<SemanticBranchNode>(result);
        var branch = (SemanticBranchNode)result;
        Assert.Single(branch.Children);
        Assert.Equal("name", branch.Children[0].SemanticId);
    }

    [Fact]
    public void ParseJson_ShouldParseNestedJsonString_WhenRootIsString()
    {
        const string Json = """
                            {
                            "items": {
                            "item":{
                            "key" : "value"
                            }
                            }
                            }
                            """;
        using var doc = JsonDocument.Parse(Json);

        var result = JsonConverter.ParseJson(doc);

        Assert.IsType<SemanticBranchNode>(result);
        var branch = (SemanticBranchNode)result;
        Assert.Single(branch.Children);
        Assert.Equal("item", branch.Children[0].SemanticId);
    }

    [Fact]
    public void ParseJson_ShouldReturnArrayBranch_WhenJsonIsArray()
    {
        const string Json = "[{\"item\": 1}, {\"item\": 2}]";
        using var doc = JsonDocument.Parse(Json);

        var result = JsonConverter.ParseJson(doc);

        Assert.IsType<SemanticBranchNode>(result);
        var branch = (SemanticBranchNode)result;
        Assert.Equal(2, branch.Children.Count);
    }

    [Fact]
    public void ParseJson_ShouldReturnArrayBranch_WhenJsonIsMultipleArray()
    {
        const string Json = "[{\"item\": 1}, {\"item\": 2}, {\"item\": 3}]";
        using var doc = JsonDocument.Parse(Json);

        var result = JsonConverter.ParseJson(doc);

        Assert.IsType<SemanticBranchNode>(result);
        var branch = (SemanticBranchNode)result;
        Assert.Equal(3, branch.Children.Count);
    }

    [Fact]
    public void ParseJson_ShouldHandleEmptyObject()
    {
        const string Json = "{}";
        using var doc = JsonDocument.Parse(Json);

        var result = JsonConverter.ParseJson(doc);

        Assert.IsType<SemanticBranchNode>(result);
        var branch = (SemanticBranchNode)result;
        Assert.Empty(branch.Children);
    }

    [Fact]
    public void ParseJson_ShouldProcessArrayInsideObjectProperty()
    {
        const string Json = """
                            {
                                "items": [
                                    { "id": 1 },
                                    { "id": 2 }
                                ]
                            }
                            """;
        using var doc = JsonDocument.Parse(Json);

        var result = JsonConverter.ParseJson(doc);

        Assert.IsType<SemanticBranchNode>(result);
        var branch = (SemanticBranchNode)result;
        Assert.Equal(2, branch.Children.Count);

        var itemsBranch = branch.Children[0] as SemanticBranchNode;
        Assert.NotNull(itemsBranch);
        Assert.Equal("items", itemsBranch.SemanticId);
        Assert.Single(itemsBranch.Children);
    }

    [Fact]
    public void ParseJson_ShouldProcessArrayAsSinglePropertyValue()
    {
        const string Json = """
                            {
                                "data": [
                                    { "name": "A" },
                                    { "name": "B" }
                                ]
                            }
                            """;
        using var doc = JsonDocument.Parse(Json);

        var result = JsonConverter.ParseJson(doc);

        Assert.IsType<SemanticBranchNode>(result);
        var branch = (SemanticBranchNode)result;
        Assert.Equal("data", branch.SemanticId);
        Assert.Equal(2, branch.Children.Count);

        var firstItem = branch.Children[0] as SemanticBranchNode;
        Assert.NotNull(firstItem);
        Assert.Single(firstItem.Children);
        Assert.Equal("name", firstItem.Children[0].SemanticId);
    }

    [Fact]
    public void ParseJson_ShouldHandleNullString()
    {
        const string Json = "null";
        using var doc = JsonDocument.Parse(Json);

        var result = JsonConverter.ParseJson(doc);

        Assert.IsType<SemanticLeafNode>(result);
    }
}

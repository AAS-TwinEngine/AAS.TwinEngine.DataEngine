using System.Text.Json;
using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.Infrastructure.Shared;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Shared;

public class AasJsonNodeDeserializerTests
{
    [Fact]
    public void UnwrapResult_ReturnsResultNode_WhenResultPropertyExists()
    {
        var root = JsonNode.Parse("""
                                  {
                                    "result": {
                                      "id": "shell-1"
                                    }
                                  }
                                  """);

        var result = AasJsonNodeDeserializer.UnwrapResult(root);

        Assert.NotNull(result);
        Assert.Equal("shell-1", result!["id"]!.GetValue<string>());
    }

    [Fact]
    public void UnwrapResult_ReturnsRootNode_WhenResultPropertyDoesNotExist()
    {
        var root = JsonNode.Parse("""
                                  {
                                    "id": "shell-2"
                                  }
                                  """);

        var result = AasJsonNodeDeserializer.UnwrapResult(root);

        Assert.Same(root, result);
    }

    [Fact]
    public void DeserializeAasNode_ReturnsNull_WhenNodeIsNull()
    {
        var result = AasJsonNodeDeserializer.DeserializeAasNode<string>(null, _ => "value");

        Assert.Null(result);
    }

    [Fact]
    public void DeserializeAasNode_ReturnsDeserializedValue_WhenNodeIsProvided()
    {
        var node = JsonNode.Parse("""{ "value": "abc" }""");

        var result = AasJsonNodeDeserializer.DeserializeAasNode(node, n => n["value"]?.GetValue<string>());

        Assert.Equal("abc", result);
    }

    [Fact]
    public void DeserializeAasArray_ReturnsNull_WhenNodeIsNotArray()
    {
        var node = JsonNode.Parse("""{ "name": "not-an-array" }""");

        var result = AasJsonNodeDeserializer.DeserializeAasArray(node, n => n["name"]?.GetValue<string>());

        Assert.Null(result);
    }

    [Fact]
    public void DeserializeAasArray_ReturnsFilteredArray_WhenArrayContainsNullEntries()
    {
        var node = JsonNode.Parse("""
                                  [
                                    { "name": "one" },
                                    null,
                                    { "name": "two" }
                                  ]
                                  """);

        var result = AasJsonNodeDeserializer.DeserializeAasArray(node, n => n["name"]?.GetValue<string>());

        Assert.NotNull(result);
        Assert.Equal(2, result!.Count);
        Assert.Equal("one", result[0]);
        Assert.Equal("two", result[1]);
    }

    [Fact]
    public void DeserializeObject_ReturnsDefault_WhenNodeIsNull()
    {
        var result = AasJsonNodeDeserializer.DeserializeObject<TestDto>(null);

        Assert.Null(result);
    }

    [Fact]
    public void DeserializeObject_UsesDefaultOptions_WhenOptionsAreNotProvided()
    {
        var node = JsonNode.Parse("""{ "name": "Default" }""");

        var result = AasJsonNodeDeserializer.DeserializeObject<TestDto>(node);

        Assert.NotNull(result);
        Assert.Equal("Default", result!.Name);
    }

    [Fact]
    public void DeserializeObject_UsesProvidedOptions_WhenOptionsAreProvided()
    {
        var node = JsonNode.Parse("""{ "NAME": "CaseInsensitive" }""");
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var result = AasJsonNodeDeserializer.DeserializeObject<TestDto>(node, options);

        Assert.NotNull(result);
        Assert.Equal("CaseInsensitive", result!.Name);
    }

    [Fact]
    public void DeserializeEnum_ReturnsNull_WhenNodeIsNull()
    {
        var result = AasJsonNodeDeserializer.DeserializeEnum<AssetKind>(null);

        Assert.Null(result);
    }

    [Fact]
    public void DeserializeEnum_ReturnsEnumValue_WhenNodeContainsValidValue()
    {
        var node = JsonNode.Parse("\"Instance\"");

        var result = AasJsonNodeDeserializer.DeserializeEnum<AssetKind>(node);

        Assert.Equal(AssetKind.Instance, result);
    }

    [Fact]
    public void DeserializeEnum_ReturnsNull_WhenNodeContainsInvalidValue()
    {
        var node = JsonNode.Parse("\"NotARealEnumValue\"");

        var result = AasJsonNodeDeserializer.DeserializeEnum<AssetKind>(node);

        Assert.Null(result);
    }

    private sealed class TestDto
    {
        public string? Name { get; init; }
    }
}

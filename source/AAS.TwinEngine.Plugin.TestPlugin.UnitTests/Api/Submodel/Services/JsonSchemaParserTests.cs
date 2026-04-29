using System.Text.Json;
using System.Text.Json.Serialization;

using AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Services;
using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Exceptions;
using AAS.TwinEngine.Plugin.TestPlugin.DomainModel.Submodel;

using Json.Schema;

using Microsoft.Extensions.Logging;

using NSubstitute;

namespace AAS.TwinEngine.Plugin.TestPlugin.UnitTests.Api.Submodel.Services;

public class JsonSchemaParserTests
{
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    private readonly string _invalidJson = @"{""Invalid json"": {}}";

    private const string ValidationFailSchemaString = @"{ ""type"" : ""null"" }";

    private const string NoPropertiesSchemaString = @"{
            ""$schema"": ""http://json-schema.org/draft-07/schema#"",
            ""type"": ""object""
        }";

    private const string SimpleSchemaString = @"{
            ""$schema"": ""http://json-schema.org/draft-07/schema#"",
            ""type"": ""object"",
            ""properties"": {
                ""foo"": { ""type"": ""string"" }
            }}";

    private const string NestedSchemaString = @"{
            ""$schema"": ""http://json-schema.org/draft-07/schema#"",
            ""type"": ""object"",
            ""properties"": {
                ""parent"": {
                    ""type"": ""object"",
                    ""properties"": {
                        ""child"": { ""type"": ""number"" }
                    }}}}";

    private const string ArraySchemaString = @"{
            ""$schema"": ""http://json-schema.org/draft-07/schema#"",
            ""type"": ""object"",
            ""properties"": {
                ""list"": {
                    ""type"": ""array"",
                    ""properties"": { ""id"": { ""type"": ""integer"" } }
                }}}";

    private const string ArrayWithRefSchemaString = @"{
            ""$schema"": ""http://json-schema.org/draft-07/schema#"",
            ""type"": ""object"",
            ""properties"": {
                ""items"": {
                    ""type"": ""array"",
                     ""$ref"": ""#/definitions/ItemDef""
                }
            },
            ""definitions"": {
                ""ItemDef"": {
                    ""type"": ""object"",
                    ""properties"": { ""val"": { ""type"": ""integer"" } }
                }
            }
        }";

    private const string AllDataTypesSchemaWithRefString = @"{
    ""$schema"": ""http://json-schema.org/draft-07/schema#"",
    ""type"": ""object"",
    ""properties"": {
        ""root"" :{
            ""type"" : ""array"",
           ""properties"" : {
        ""stringField"": { ""type"": ""string"" },
        ""numberField"": { ""type"": ""number"" },
        ""integerField"": { ""type"": ""integer"" },
        ""booleanField"": { ""type"": ""boolean"" },
        ""arrayField"": {
            ""$ref"" : ""#/definitions/itemField""
        },
        ""objectField"": {
            ""type"": ""object"",
            ""properties"": {
                ""nestedProp"": { ""type"": ""string"" }
            }
        },
        ""nullField"": { ""type"": ""null"" }
        }
        }
    },
    ""definitions"" : {
        ""itemField"":{
        ""type"":""array"",
        ""properties"": {
            ""items"": { ""type"": ""string"" }
    }
    }
    }
    }";

    private readonly ILogger<JsonSchemaParser> _logger;
    private readonly JsonSchemaParser _sut;
    private readonly IJsonSchemaValidator _jsonSchemaValidator;

    public JsonSchemaParserTests()
    {
        _logger = Substitute.For<ILogger<JsonSchemaParser>>();
        _jsonSchemaValidator = Substitute.For<IJsonSchemaValidator>();
        _sut = new JsonSchemaParser(_logger, _jsonSchemaValidator);
    }

    [Fact]
    public void ParseJsonSchema_SchemaValidationFails_ThrowsBadRequestException()
    {
        var ValidationFailSchema = JsonSerializer.Deserialize<JsonSchema>(ValidationFailSchemaString, _options);

        Assert.Throws<BadRequestException>(() => _sut.ParseJsonSchema(ValidationFailSchema));
    }

    [Fact]
    public void ParseJsonSchema_NoRootProperties_ThrowsBadRequestException()
    {
        var NoPropertiesSchema = JsonSerializer.Deserialize<JsonSchema>(NoPropertiesSchemaString, _options);

        Assert.Throws<BadRequestException>(() => _sut.ParseJsonSchema(NoPropertiesSchema));
    }

    [Fact]
    public void ParseJsonSchema_SimpleSchema_ReturnsLeafNode()
    {
        var SimpleSchema = JsonSerializer.Deserialize<JsonSchema>(SimpleSchemaString, _options);

        var node = _sut.ParseJsonSchema(SimpleSchema);

        Assert.NotNull(node);
        Assert.IsType<SemanticLeafNode>(node);
        var leaf = (SemanticLeafNode)node;
        Assert.Equal("foo", leaf.SemanticId);
        Assert.Equal(string.Empty, leaf.Value);
    }

    [Fact]
    public void ParseJsonSchema_NestedObject_ReturnsBranchNodeWithChild()
    {
        var NestedSchema = JsonSerializer.Deserialize<JsonSchema>(NestedSchemaString, _options);

        var node = _sut.ParseJsonSchema(NestedSchema);

        Assert.NotNull(node);
        Assert.IsType<SemanticBranchNode>(node);
        var branch = (SemanticBranchNode)node;
        Assert.Equal("parent", branch.SemanticId);
        Assert.Single(branch.Children);
        var child = branch.Children[0] as SemanticLeafNode;
        Assert.NotNull(child);
        Assert.Equal("child", child.SemanticId);
    }

    [Fact]
    public void ParseJsonSchema_ArrayOfObjects_ReturnsBranchNodeWithLeafChild()
    {
        var arraySchema = JsonSerializer.Deserialize<JsonSchema>(ArraySchemaString, _options);

        var node = _sut.ParseJsonSchema(arraySchema!);

        Assert.NotNull(node);
        Assert.IsType<SemanticBranchNode>(node);
        var branch = (SemanticBranchNode)node;
        Assert.Equal("list", branch.SemanticId);
        Assert.Single(branch.Children);
        var child = branch.Children[0] as SemanticLeafNode;
        Assert.NotNull(child);
        Assert.Equal("id", child.SemanticId);
    }

    [Fact]
    public void ParseJsonSchema_ArrayWithRef_ReturnsBranchNodeWithLeafChild()
    {
        var arrayWithRefSchema = JsonSerializer.Deserialize<JsonSchema>(ArrayWithRefSchemaString, _options);

        var node = _sut.ParseJsonSchema(arrayWithRefSchema!);

        Assert.NotNull(node);
        Assert.IsType<SemanticBranchNode>(node);
        var branch = (SemanticBranchNode)node;
        Assert.Equal("items", branch.SemanticId);
        Assert.Single(branch.Children);
        var child = branch.Children[0] as SemanticLeafNode;
        Assert.NotNull(child);
        Assert.Equal("val", child.SemanticId);
    }

    [Fact]
    public void ParseJsonSchema_AllDataTypeSchemaWithRef_ReturnsBranchNodeWithLeafChild()
    {
        var allDataTypesSchemaWithRef = JsonSerializer.Deserialize<JsonSchema>(AllDataTypesSchemaWithRefString, _options);

        var node = _sut.ParseJsonSchema(allDataTypesSchemaWithRef!);

        Assert.NotNull(node);
        Assert.IsType<SemanticBranchNode>(node);
        var branch = (SemanticBranchNode)node;
        Assert.Equal("root", branch.SemanticId);
        Assert.Equal(DataType.Array, branch.DataType);
        var child1 = branch.Children[0] as SemanticLeafNode;
        Assert.Equal("stringField", child1!.SemanticId);
        Assert.Equal(DataType.String, child1.DataType);
        var child2 = branch.Children[1] as SemanticLeafNode;
        Assert.Equal("numberField", child2!.SemanticId);
        Assert.Equal(DataType.Number, child2.DataType);
        var child3 = branch.Children[2] as SemanticLeafNode;
        Assert.Equal("integerField", child3!.SemanticId);
        Assert.Equal(DataType.Integer, child3.DataType);
        var child4 = branch.Children[3] as SemanticLeafNode;
        Assert.Equal("booleanField", child4!.SemanticId);
        Assert.Equal(DataType.Boolean, child4.DataType);
        var branch1 = branch.Children[4] as SemanticBranchNode;
        Assert.Equal("arrayField", branch1?.SemanticId);
        Assert.Equal(DataType.Array, branch1?.DataType);
        var leaf1 = branch1!.Children[0] as SemanticLeafNode;
        Assert.Equal("items", leaf1!.SemanticId);
        Assert.Equal(DataType.String, leaf1.DataType);
    }

    [Fact]
    public void ParseJsonSchema_ReferenceNotFound_ReturnsLeafNode()
    {
        const string SchemaString = @"{
        ""$schema"": ""http://json-schema.org/draft-07/schema#"",
        ""type"": ""object"",
        ""properties"": {
            ""mystery"": { ""$ref"": ""#/definitions/DoesNotExist"" }
        }
    }";
        var schema = JsonSerializer.Deserialize<JsonSchema>(SchemaString, _options);

        var node = _sut.ParseJsonSchema(schema!);

        Assert.NotNull(node);
        Assert.IsType<SemanticLeafNode>(node);
        var leaf = (SemanticLeafNode)node;
        Assert.Equal("mystery", leaf.SemanticId);
        Assert.Equal(DataType.Unknown, leaf.DataType);
    }

    [Fact]
    public void ParseJsonSchema_ReferenceToObjectDefinition_ReturnsBranchNode()
    {
        const string SchemaString = @"{
        ""$schema"": ""http://json-schema.org/draft-07/schema#"",
        ""type"": ""object"",
        ""properties"": {
            ""person"": { ""$ref"": ""#/definitions/Person"" }
        },
        ""definitions"": {
            ""Person"": {
                ""type"": ""object"",
                ""properties"": {
                    ""name"": { ""type"": ""string"" },
                    ""age"": { ""type"": ""integer"" }
                }
            }
        }
    }";
        var schema = JsonSerializer.Deserialize<JsonSchema>(SchemaString, _options);

        var node = _sut.ParseJsonSchema(schema);

        Assert.NotNull(node);
        Assert.IsType<SemanticBranchNode>(node);
        var branch = (SemanticBranchNode)node;
        Assert.Equal("person", branch.SemanticId);
        Assert.Equal(DataType.Object, branch.DataType);
        Assert.Collection(branch.Children,
            child =>
            {
                var leaf = Assert.IsType<SemanticLeafNode>(child);
                Assert.Equal("name", leaf.SemanticId);
                Assert.Equal(DataType.String, leaf.DataType);
            },
            child =>
            {
                var leaf = Assert.IsType<SemanticLeafNode>(child);
                Assert.Equal("age", leaf.SemanticId);
                Assert.Equal(DataType.Integer, leaf.DataType);
            });
    }

    [Fact]
    public void ParseJsonSchema_ReferenceToArrayDefinition_ReturnsBranchNode()
    {
        const string SchemaString = @"{
        ""$schema"": ""http://json-schema.org/draft-07/schema#"",
        ""type"": ""object"",
        ""properties"": {
            ""primes"": { ""$ref"": ""#/definitions/PrimeList"" }
        },
        ""definitions"": {
            ""PrimeList"": {
                ""type"": ""array""
                }
            }
        }";
        var schema = JsonSerializer.Deserialize<JsonSchema>(SchemaString, _options);

        var node = _sut.ParseJsonSchema(schema);

        Assert.NotNull(node);
        Assert.IsType<SemanticBranchNode>(node);
        var branch = (SemanticBranchNode)node;
        Assert.Equal("primes", branch.SemanticId);
        Assert.Equal(DataType.Array, branch.DataType);
        Assert.Empty(branch.Children);
    }

    [Fact]
    public void ParseJsonSchema_ReferenceToLeafNodeDefinition_ReturnsLeafNode()
    {
        const string SchemaString = @"{
        ""$schema"": ""http://json-schema.org/draft-07/schema#"",
        ""type"": ""object"",
        ""properties"": {
            ""person"": { ""$ref"": ""#/definitions/Person"" }
        },
        ""definitions"": {
            ""Person"": {
                ""type"": ""string""
            }
        }
    }";
        var schema = JsonSerializer.Deserialize<JsonSchema>(SchemaString, _options);

        var node = _sut.ParseJsonSchema(schema);

        Assert.NotNull(node);
        Assert.IsType<SemanticLeafNode>(node);
        var branch = (SemanticLeafNode)node;
        Assert.Equal("person", branch.SemanticId);
        Assert.Equal(DataType.String, branch.DataType);
    }

    [Fact]
    public void ParseJsonSchema_InlineObjectWithNoProperties_CoversProcessObjectFallback()
    {
        const string SchemaString = @"{
        ""$schema"": ""http://json-schema.org/draft-07/schema#"",
        ""type"": ""object"",
        ""properties"": {
            ""outer"": {
                ""type"": ""object"",
                ""properties"": {
                    ""inner"": { ""type"": ""object"" }
                }
            }
        }
    }";
        var schema = JsonSerializer.Deserialize<JsonSchema>(SchemaString, _options);

        var root = _sut.ParseJsonSchema(schema);

        var outerBranch = Assert.IsType<SemanticBranchNode>(root);
        Assert.Equal("outer", outerBranch.SemanticId);
        Assert.Single(outerBranch.Children);
        var innerBranch = Assert.IsType<SemanticBranchNode>(outerBranch.Children[0]);
        Assert.Equal("inner", innerBranch.SemanticId);
        Assert.Empty(innerBranch.Children);
    }

    [Fact]
    public void ParseJsonSchema_Draft202012WithDefsReference_ReturnsBranchNode()
    {
        const string SchemaString = @"{
        ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
        ""type"": ""object"",
        ""properties"": {
            ""person"": { ""$ref"": ""#/$defs/Person"" }
        },
        ""$defs"": {
            ""Person"": {
                ""type"": ""object"",
                ""properties"": {
                    ""name"": { ""type"": ""string"" }
                }
            }
        }
    }";
        var schema = JsonSerializer.Deserialize<JsonSchema>(SchemaString, _options);

        var node = _sut.ParseJsonSchema(schema!);

        var branch = Assert.IsType<SemanticBranchNode>(node);
        Assert.Equal("person", branch.SemanticId);
        Assert.Single(branch.Children);
        var child = Assert.IsType<SemanticLeafNode>(branch.Children[0]);
        Assert.Equal("name", child.SemanticId);
        Assert.Equal(DataType.String, child.DataType);
    }

    [Fact]
    public void ParseJsonSchema_ArrayWithItemsWrapper_ReturnsProperArrayBranch()
    {
        const string ProperArraySchemaString = @"{
            ""$schema"": ""http://json-schema.org/draft-07/schema#"",
            ""type"": ""object"",
            ""properties"": {
                ""contactList"": {
                    ""type"": ""array"",
                    ""items"": {
                        ""type"": ""object"",
                        ""properties"": {
                            ""name"": { ""type"": ""string"" },
                            ""phoneNumber"": { ""type"": ""string"" }
                        },
                        ""required"": [""name""]
                    }
                }
            }
        }";
        var schema = JsonSerializer.Deserialize<JsonSchema>(ProperArraySchemaString, _options);

        var node = _sut.ParseJsonSchema(schema!);

        Assert.NotNull(node);
        Assert.IsType<SemanticBranchNode>(node);
        var contactListBranch = (SemanticBranchNode)node;
        Assert.Equal("contactList", contactListBranch.SemanticId);
        Assert.Equal(DataType.Array, contactListBranch.DataType);
        Assert.Equal(2, contactListBranch.Children.Count);

        var nameChild = contactListBranch.Children[0] as SemanticLeafNode;
        Assert.NotNull(nameChild);
        Assert.Equal("name", nameChild.SemanticId);
        Assert.Equal(DataType.String, nameChild.DataType);

        var phoneChild = contactListBranch.Children[1] as SemanticLeafNode;
        Assert.NotNull(phoneChild);
        Assert.Equal("phoneNumber", phoneChild.SemanticId);
        Assert.Equal(DataType.String, phoneChild.DataType);
    }

    [Fact]
    public void ParseJsonSchema_ArrayWithoutItemsButWithProperties_UsesLegacyFallback()
    {
        const string LegacyArraySchemaString = @"{
            ""$schema"": ""http://json-schema.org/draft-07/schema#"",
            ""type"": ""object"",
            ""properties"": {
                ""contactList"": {
                    ""type"": ""array"",
                    ""properties"": {
                        ""name"": { ""type"": ""string"" },
                        ""phoneNumber"": { ""type"": ""string"" }
                    },
                    ""required"": [""name""]
                }
            }
        }";

        var schema = JsonSerializer.Deserialize<JsonSchema>(LegacyArraySchemaString, _options);

        var node = _sut.ParseJsonSchema(schema!);

        var contactListBranch = Assert.IsType<SemanticBranchNode>(node);
        Assert.Equal("contactList", contactListBranch.SemanticId);
        Assert.Equal(DataType.Array, contactListBranch.DataType);
        Assert.Equal(2, contactListBranch.Children.Count);
        Assert.Equal(DataType.String, contactListBranch.Children[0].DataType);
        Assert.Equal(DataType.String, contactListBranch.Children[1].DataType);
    }

    [Theory]
    [InlineData("http://json-schema.org/draft-07/schema#")]
    [InlineData("https://json-schema.org/draft/2020-12/schema")]
    public void ParseJsonSchema_SimpleSchemaAcrossDrafts_ReturnsLeafNode(string draft)
    {
        var schema = JsonSchema.FromText($$"""
                        {
                            "$schema": "{{draft}}",
                            "type": "object",
                            "properties": {
                                "foo": { "type": "string" }
                            }
                        }
                        """);

        var node = _sut.ParseJsonSchema(schema);

        var leaf = Assert.IsType<SemanticLeafNode>(node);
        Assert.Equal("foo", leaf.SemanticId);
        Assert.Equal(DataType.String, leaf.DataType);
    }

    [Fact]
    public void ParseJsonSchema_ArrayOfPrimitive_ReturnsLeaf()
    {
        var schema = JsonSchema.FromText(
                """
                        {
                            "type": "object",
                            "properties": {
                                "tags": {
                                    "type": "array",
                                    "items": { "type": "string" }
                                }
                            }
                        }
                        """);

        var result = _sut.ParseJsonSchema(schema);

        var leaf = Assert.IsType<SemanticLeafNode>(result);
        Assert.Equal("tags", leaf.SemanticId);
        Assert.Equal(DataType.String, leaf.DataType);
    }

    [Fact]
    public void ParseJsonSchema_MultipleRootProperties_OnlyFirstIsUsed()
    {
        var schema = JsonSchema.FromText(
                """
                        {
                            "type": "object",
                            "properties": {
                                "first": { "type": "string" },
                                "second": { "type": "integer" }
                            }
                        }
                        """);

        var result = _sut.ParseJsonSchema(schema);

        var leaf = Assert.IsType<SemanticLeafNode>(result);
        Assert.Equal("first", leaf.SemanticId);
    }

    [Fact]
    public void ParseJsonSchema_InvalidRefFormat_ReturnsUnknown()
    {
        var schema = JsonSchema.FromText(
                """
                        {
                            "type": "object",
                            "properties": {
                                "x": { "$ref": "#/invalid/A" }
                            }
                        }
                        """);

        var result = _sut.ParseJsonSchema(schema);

        var leaf = Assert.IsType<SemanticLeafNode>(result);
        Assert.Equal(DataType.Unknown, leaf.DataType);
    }

    [Fact]
    public void ParseJsonSchema_ArrayItemsEmptyObject_ReturnsItemLeaf()
    {
        var schema = JsonSchema.FromText(
                """
                        {
                            "type": "object",
                            "properties": {
                                "arr": {
                                    "type": "array",
                                    "items": {}
                                }
                            }
                        }
                        """);

        var result = _sut.ParseJsonSchema(schema);

        var branch = Assert.IsType<SemanticBranchNode>(result);
        Assert.Equal("arr", branch.SemanticId);
        Assert.Equal(DataType.Array, branch.DataType);
        var itemNode = Assert.IsType<SemanticLeafNode>(Assert.Single(branch.Children));
        Assert.Equal("item", itemNode.SemanticId);
        Assert.Equal(DataType.String, itemNode.DataType);
    }

    [Fact]
    public void ParseJsonSchema_ArrayOfArray_FlattensInner()
    {
        var schema = JsonSchema.FromText(
                """
                        {
                            "type": "object",
                            "properties": {
                                "arr": {
                                    "type": "array",
                                    "items": {
                                        "type": "array",
                                        "items": {
                                            "type": "string"
                                        }
                                    }
                                }
                            }
                        }
                        """);

        var result = _sut.ParseJsonSchema(schema);

        var branch = Assert.IsType<SemanticBranchNode>(result);
        var child = Assert.IsType<SemanticLeafNode>(branch.Children.First());
        Assert.Equal("arr", branch.SemanticId);
        Assert.Equal(DataType.String, child.DataType);
    }

    [Fact]
    public void ParseJsonSchema_RefToPrimitive_ReturnsLeaf()
    {
        var schema = JsonSchema.FromText(
                """
                        {
                            "type": "object",
                            "properties": {
                                "x": { "$ref": "#/$defs/A" }
                            },
                            "$defs": {
                                "A": { "type": "string" }
                            }
                        }
                        """);

        var result = _sut.ParseJsonSchema(schema);

        var leaf = Assert.IsType<SemanticLeafNode>(result);
        Assert.Equal(DataType.String, leaf.DataType);
    }
}

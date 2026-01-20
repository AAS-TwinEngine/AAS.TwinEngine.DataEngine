using System.Text.Json;
using System.Text.Json.Serialization;

using Aas.TwinEngine.Plugin.TestPlugin.Api.Submodel.Services;
using Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Exceptions;
using Aas.TwinEngine.Plugin.TestPlugin.DomainModel.Submodel;

using Json.Schema;

using Microsoft.Extensions.Logging;

using NSubstitute;

namespace Aas.TwinEngine.Plugin.TestPlugin.UnitTests.Api.Submodel.Services;

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

    public JsonSchemaParserTests()
    {
        _logger = Substitute.For<ILogger<JsonSchemaParser>>();
        _sut = new JsonSchemaParser(_logger);
    }

    [Fact]
    public void ParseJsonSchema_InvalidJson_ThrowsBadRequestException()
    {
        var InvalidJsonSchema = JsonSerializer.Deserialize<JsonSchema>(_invalidJson, _options);

        Assert.Throws<BadRequestException>(() => _sut.ParseJsonSchema(InvalidJsonSchema));
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
}

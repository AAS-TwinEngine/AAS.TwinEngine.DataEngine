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

    private const string ValidationFailSchemaString = @"{ ""type"" : ""null"" }";

    private const string NoPropertiesSchemaString = @"{
        ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
        ""type"": ""object""
    }";

    private const string SimpleSchemaString = @"{
        ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
        ""type"": ""object"",
        ""properties"": {
            ""foo"": { ""type"": ""string"" }
        }}";

    private const string NestedSchemaString = @"{
        ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
        ""type"": ""object"",
        ""properties"": {
            ""parent"": {
                ""type"": ""object"",
                ""properties"": {
                    ""child"": { ""type"": ""number"" }
                }}}}";

    private const string ArraySchemaString = @"{
        ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
        ""type"": ""object"",
        ""properties"": {
            ""list"": {
                ""type"": ""array"",
                ""items"": {
                    ""type"": ""object"",
                    ""properties"": { ""id"": { ""type"": ""integer"" } }
                }
            }}}";

    private const string ArrayWithRefSchemaString = @"{
        ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
        ""type"": ""object"",
        ""properties"": {
            ""items"": {
                ""type"": ""array"",
                ""items"": { ""$ref"": ""#/$defs/ItemDef"" }
            }
        },
        ""$defs"": {
            ""ItemDef"": {
                ""type"": ""object"",
                ""properties"": { ""val"": { ""type"": ""integer"" } }
            }
        }
    }";

    private const string AllDataTypesSchemaWithRefString = @"{
        ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
        ""type"": ""object"",
        ""properties"": {
            ""root"" :{
                ""type"" : ""array"",
                ""items"" : {
                    ""type"": ""object"",
                    ""properties"" : {
                        ""stringField"": { ""type"": ""string"" },
                        ""numberField"": { ""type"": ""number"" },
                        ""integerField"": { ""type"": ""integer"" },
                        ""booleanField"": { ""type"": ""boolean"" },
                        ""arrayField"": {
                            ""$ref"" : ""#/$defs/itemField""
                        },
                        ""objectField"": {
                            ""type"": ""object"",
                            ""properties"": {
                                ""nestedProp"": { ""type"": ""string"" }
                            }
                        }
                    }
                }
            }
        },
        ""$defs"" : {
            ""itemField"":{
                ""type"":""array"",
                ""items"": {
                    ""type"": ""string""
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
    public void ParseJsonSchema_SchemaValidationFails_ThrowsBadRequestException()
    {
        var schema = JsonSerializer.Deserialize<JsonSchema>(ValidationFailSchemaString, _options);
        Assert.Throws<BadRequestException>(() => _sut.ParseJsonSchema(schema));
    }

    [Fact]
    public void ParseJsonSchema_NoRootProperties_ThrowsBadRequestException()
    {
        var schema = JsonSerializer.Deserialize<JsonSchema>(NoPropertiesSchemaString, _options);
        Assert.Throws<BadRequestException>(() => _sut.ParseJsonSchema(schema));
    }

    [Fact]
    public void ParseJsonSchema_SimpleSchema_ReturnsLeafNode()
    {
        var schema = JsonSerializer.Deserialize<JsonSchema>(SimpleSchemaString, _options);

        var node = _sut.ParseJsonSchema(schema);

        var leaf = Assert.IsType<SemanticLeafNode>(node);
        Assert.Equal("foo", leaf.SemanticId);
    }

    [Theory]
    [InlineData("http://json-schema.org/draft-07/schema#")]
    [InlineData("https://json-schema.org/draft/2019-09/schema")]
    [InlineData("https://json-schema.org/draft/2020-12/schema")]
    public void ParseJsonSchema_SimpleSchemaAcrossDrafts_ReturnsLeafNode(string draft)
    {
        var schema = JsonSchema.FromText($@"{{
            ""$schema"": ""{draft}"",
            ""type"": ""object"",
            ""properties"": {{
                ""foo"": {{ ""type"": ""string"" }}
            }}
        }}");

        var node = _sut.ParseJsonSchema(schema);

        var leaf = Assert.IsType<SemanticLeafNode>(node);
        Assert.Equal("foo", leaf.SemanticId);
        Assert.Equal(DataType.String, leaf.DataType);
    }

    [Fact]
    public void ParseJsonSchema_NestedObject_ReturnsBranchNodeWithChild()
    {
        var schema = JsonSerializer.Deserialize<JsonSchema>(NestedSchemaString, _options);

        var node = _sut.ParseJsonSchema(schema);

        var branch = Assert.IsType<SemanticBranchNode>(node);
        Assert.Equal("parent", branch.SemanticId);

        var child = Assert.IsType<SemanticLeafNode>(branch.Children[0]);
        Assert.Equal("child", child.SemanticId);
    }

    [Fact]
    public void ParseJsonSchema_ArrayOfObjects_ReturnsBranchNodeWithLeafChild()
    {
        var schema = JsonSerializer.Deserialize<JsonSchema>(ArraySchemaString, _options);

        var node = _sut.ParseJsonSchema(schema);

        var branch = Assert.IsType<SemanticBranchNode>(node);
        Assert.Equal("list", branch.SemanticId);

        var child = Assert.IsType<SemanticLeafNode>(branch.Children[0]);
        Assert.Equal("id", child.SemanticId);
    }

    [Fact]
    public void ParseJsonSchema_ArrayWithRef_ReturnsBranchNodeWithLeafChild()
    {
        var schema = JsonSerializer.Deserialize<JsonSchema>(ArrayWithRefSchemaString, _options);

        var node = _sut.ParseJsonSchema(schema);

        var branch = Assert.IsType<SemanticBranchNode>(node);
        Assert.Equal("items", branch.SemanticId);

        var child = Assert.IsType<SemanticLeafNode>(branch.Children[0]);
        Assert.Equal("val", child.SemanticId);
    }

    [Fact]
    public void ParseJsonSchema_Draft7ArrayWithDefinitionsRef_ReturnsBranchNodeWithLeafChild()
    {
        var schema = JsonSchema.FromText(@"{
            ""$schema"": ""http://json-schema.org/draft-07/schema#"",
            ""type"": ""object"",
            ""properties"": {
                ""items"": {
                    ""type"": ""array"",
                    ""items"": { ""$ref"": ""#/definitions/ItemDef"" }
                }
            },
            ""definitions"": {
                ""ItemDef"": {
                    ""type"": ""object"",
                    ""properties"": {
                        ""value"": { ""type"": ""integer"" }
                    }
                }
            }
        }");

        var node = _sut.ParseJsonSchema(schema);

        var branch = Assert.IsType<SemanticBranchNode>(node);
        Assert.Equal("items", branch.SemanticId);
        var child = Assert.IsType<SemanticLeafNode>(branch.Children[0]);
        Assert.Equal("value", child.SemanticId);
        Assert.Equal(DataType.Integer, child.DataType);
    }

    [Fact]
    public void ParseJsonSchema_Draft7SchemaWithDefsKeyword_ReturnsLeafNode()
    {
        var schema = JsonSchema.FromText(@"{
            ""$schema"": ""http://json-schema.org/draft-07/schema#"",
            ""type"": ""object"",
            ""properties"": {
                ""item"": { ""$ref"": ""#/$defs/ItemDef"" }
            },
            ""$defs"": {
                ""ItemDef"": {
                    ""type"": ""object"",
                    ""properties"": {
                        ""value"": { ""type"": ""string"" }
                    }
                }
            }
        }");

        var node = _sut.ParseJsonSchema(schema);

        var branch = Assert.IsType<SemanticBranchNode>(node);
        var child = Assert.IsType<SemanticLeafNode>(branch.Children[0]);
        Assert.Equal("value", child.SemanticId);
        Assert.Equal(DataType.String, child.DataType);
    }

    [Fact]
    public void ParseJsonSchema_Draft202012SchemaWithDefinitionsKeyword_ReturnsLeafNode()
    {
        var schema = JsonSchema.FromText(@"{
            ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
            ""type"": ""object"",
            ""properties"": {
                ""item"": { ""$ref"": ""#/definitions/ItemDef"" }
            },
            ""definitions"": {
                ""ItemDef"": {
                    ""type"": ""object"",
                    ""properties"": {
                        ""value"": { ""type"": ""integer"" }
                    }
                }
            }
        }");

        var node = _sut.ParseJsonSchema(schema);

        var branch = Assert.IsType<SemanticBranchNode>(node);
        var child = Assert.IsType<SemanticLeafNode>(branch.Children[0]);
        Assert.Equal("value", child.SemanticId);
        Assert.Equal(DataType.Integer, child.DataType);
    }

    [Fact]
    public void ParseJsonSchema_AllDataTypeSchemaWithRef_ReturnsBranchNode()
    {
        var schema = JsonSerializer.Deserialize<JsonSchema>(AllDataTypesSchemaWithRefString, _options);

        var node = _sut.ParseJsonSchema(schema);

        var branch = Assert.IsType<SemanticBranchNode>(node);
        Assert.Equal("root", branch.SemanticId);
        Assert.Equal(DataType.Array, branch.DataType);
    }

    [Fact]
    public void ParseJsonSchema_ReferenceNotFound_ReturnsLeafNode()
    {
        const string Schema = @"{
            ""type"": ""object"",
            ""properties"": {
                ""mystery"": { ""$ref"": ""#/$defs/DoesNotExist"" }
            }
        }";

        var schema = JsonSerializer.Deserialize<JsonSchema>(Schema, _options);

        var node = _sut.ParseJsonSchema(schema);

        var leaf = Assert.IsType<SemanticLeafNode>(node);
        Assert.Equal("mystery", leaf.SemanticId);
        Assert.Equal(DataType.Unknown, leaf.DataType);
    }

    [Fact]
    public void ParseJsonSchema_MissingType_DefaultsToString()
    {
        var schema = JsonSchema.FromText(@"{
            ""type"": ""object"",
            ""properties"": {
                ""unknown"": {}
            }
        }");

        var node = _sut.ParseJsonSchema(schema);

        var leaf = Assert.IsType<SemanticLeafNode>(node);
        Assert.Equal(DataType.String, leaf.DataType);
    }

    [Fact]
    public void ParseJsonSchema_InvalidRef_ReturnsUnknownLeaf()
    {
        var schema = JsonSchema.FromText(@"{
            ""type"": ""object"",
            ""properties"": {
                ""bad"": { ""$ref"": ""#/$defs/Unknown"" }
            }
        }");

        var node = _sut.ParseJsonSchema(schema);

        var leaf = Assert.IsType<SemanticLeafNode>(node);
        Assert.Equal(DataType.Unknown, leaf.DataType);
    }

    [Fact]
    public void ParseJsonSchema_DeepNestedDefs_ResolvesCorrectly()
    {
        var schema = JsonSchema.FromText(@"{
            ""type"": ""object"",
            ""properties"": {
                ""root"": { ""$ref"": ""#/$defs/A"" }
            },
            ""$defs"": {
                ""A"": {
                    ""type"": ""object"",
                    ""properties"": {
                        ""child"": { ""$ref"": ""#/$defs/B"" }
                    }
                },
                ""B"": {
                    ""type"": ""string""
                }
            }
        }");

        var node = _sut.ParseJsonSchema(schema);

        var branch = Assert.IsType<SemanticBranchNode>(node);
        var child = Assert.IsType<SemanticLeafNode>(branch.Children[0]);
        Assert.Equal("child", child.SemanticId);
        Assert.Equal(DataType.String, child.DataType);
    }
}

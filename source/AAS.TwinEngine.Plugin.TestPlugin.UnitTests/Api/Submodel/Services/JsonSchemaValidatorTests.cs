using AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Services;
using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Exceptions;
using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.Submodel.Config;

using Json.Schema;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace AAS.TwinEngine.Plugin.TestPlugin.UnitTests.Api.Submodel.Services;

public class JsonSchemaValidatorTests
{
    private readonly JsonSchemaValidator _sut;
    private readonly ILogger<JsonSchemaValidator> _logger;

    public static IEnumerable<object[]> InvalidPrimitives => [
        [SchemaValueType.String,  "name",  123],
        [SchemaValueType.Integer, "count", 12.34],
        [SchemaValueType.Number,  "price", "19.99a"],
        [SchemaValueType.Boolean, "flag",  "flase"],
        [SchemaValueType.Number,  "age",   "8o5"],
        [SchemaValueType.Number,  "age",   "-10n5"],
        [SchemaValueType.Integer, "name",  "10o"],
        [SchemaValueType.Boolean, "flag",  "\"true\""]
    ];

    public JsonSchemaValidatorTests()
    {
        var semantics = Substitute.For<IOptions<Semantics>>();
        semantics.Value.Returns(new Semantics
        {
            IndexContextPrefix = "_aastwinengine_"
        });
        _logger = Substitute.For<ILogger<JsonSchemaValidator>>();
        _sut = new JsonSchemaValidator(semantics, _logger);
    }

    [Fact]
    public void ValidateResponseContent_EmptyResponse_ThrowsBadRequest()
    {
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object).Build();

        Assert.Throws<NotFoundException>(() => _sut.ValidateResponseContent("", schema));
    }

    [Fact]
    public void ValidateRequestSchema_NullSchema_ThrowsInvalidUserInputException()
    {
        Assert.Throws<BadRequestException>(() => _sut.ValidateRequestSchema(null!));
        _logger.Received(1).Log(
                                LogLevel.Error,
                                Arg.Any<EventId>(),
                                Arg.Any<object>(),
                                Arg.Any<Exception>(),
                                Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void ValidateResponseContent_ValidateJsonSchemaRemovePrefix_DoesNotThrow()
    {
        var schema = new JsonSchemaBuilder()
        .Type(SchemaValueType.Object)
        .Properties(new Dictionary<string, JsonSchemaBuilder>
        {
            ["ContactInformation_aastwinengine_00"] = new JsonSchemaBuilder().Type(SchemaValueType.Object)
        })
        .Required("ContactInformation_aastwinengine_00")
        .Build();

        const string Json = "{\"ContactInformation\": {}}";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Fact]
    public void ValidateResponseContent_ValidJsonAndSchema_DoesNotThrow()
    {
        var schema = new JsonSchemaBuilder()
        .Type(SchemaValueType.Object)
        .Properties(new Dictionary<string, JsonSchemaBuilder>
        {
            ["name"] = new JsonSchemaBuilder().Type(SchemaValueType.String)
        })
        .Required("name")
        .Build();

        const string Json = "{\"name\": \"Test\"}";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Fact]
    public void ValidateResponseContent_Draft7Schema_DoesNotThrow()
    {
        const string SchemaJson = """
                {
                    "$schema": "http://json-schema.org/draft-07/schema#",
                    "type": "object",
                    "properties": {
                        "contact": {
                            "type": "object",
                            "properties": {
                                "name": { "type": "string" }
                            },
                            "required": ["name"]
                        }
                    },
                    "required": ["contact"]
                }
                """;

        var schema = JsonSchema.FromText(SchemaJson);
        const string Json = "{\"contact\":{\"name\":\"Jane\"}}";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Fact]
    public void ValidateResponseContent_Draft202012Schema_DoesNotThrow()
    {
        const string SchemaJson = """
                {
                    "$schema": "https://json-schema.org/draft/2020-12/schema",
                    "type": "object",
                    "properties": {
                        "contact": {
                            "type": "object",
                            "properties": {
                                "name": { "type": "string" }
                            },
                            "required": ["name"]
                        }
                    },
                    "required": ["contact"]
                }
                """;

        var schema = JsonSchema.FromText(SchemaJson);
        const string Json = "{\"contact\":{\"name\":\"Jane\"}}";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Fact]
    public void ValidateResponseContent_WithoutSchemaKeyword_AndUsesDraft202012Keyword_DefaultsToDraft202012_DoesNotThrow()
    {
        const string SchemaJson = """
                {
                    "type": "object",
                    "properties": {
                        "contact": {
                            "type": "object",
                            "properties": {
                                "name": { "type": "string" }
                            },
                            "required": ["name"],
                            "unevaluatedProperties": false
                        }
                    },
                    "required": ["contact"],
                    "unevaluatedProperties": false
                }
                """;

        var schema = JsonSchema.FromText(SchemaJson);
        const string Json = "{\"contact\":{\"name\":\"Jane\"}}";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Theory]
    [MemberData(nameof(InvalidPrimitives))]
    public void ValidateResponseContent_InvalidValueType_ThrowsBadRequest(
        SchemaValueType expectedType,
        string property,
        string rawValue)
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                [property] = new JsonSchemaBuilder().Type(expectedType)
            })
            .Required(property)
            .Build();
        var json = $"{{\"{property}\": {rawValue} }}";

        Assert.Throws<NotFoundException>(() => _sut.ValidateResponseContent(json, schema));
    }

    [Fact]
    public void ValidateResponseContent_PropertyTypeStringOrArray_WithString_DoesNotThrow()
    {
        var schema = new JsonSchemaBuilder()
                     .Type(SchemaValueType.Object)
                     .Properties(new Dictionary<string, JsonSchemaBuilder>
                     {
                         ["value"] = new JsonSchemaBuilder().Type(SchemaValueType.String, SchemaValueType.Array)
                     })
                     .Required("value")
                     .Build();

        const string Json = "{\"value\": \"hello\"}";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Fact]
    public void ValidateResponseContent_Draft202012SchemaWithUriDefs_WhenValidatedTwice_DoesNotThrow()
    {
        const string SchemaJson = """
                {
                    "$schema": "https://json-schema.org/draft/2020-12/schema",
                    "type": "object",
                    "properties": {
                        "https://admin-shell.io/idta/CustomSubmodel/Submodel/Template/0/1": {
                            "type": "object",
                            "properties": {
                                "https://admin-shell.io/idta/HierarchicalStructures/EntryNode/1/0": {
                                    "$ref": "#/$defs/https://admin-shell.io/idta/HierarchicalStructures/EntryNode/1/0"
                                }
                            },
                            "required": [
                                "https://admin-shell.io/idta/HierarchicalStructures/EntryNode/1/0"
                            ]
                        }
                    },
                    "required": [
                        "https://admin-shell.io/idta/CustomSubmodel/Submodel/Template/0/1"
                    ],
                    "$defs": {
                        "https://admin-shell.io/idta/HierarchicalStructures/EntryNode/1/0": {
                            "type": "object",
                            "properties": {
                                "https://admin-shell.io/idta/HierarchicalStructures/EntryNode/1/0_globalAssetId": {
                                    "type": "string"
                                }
                            },
                            "required": [
                                "https://admin-shell.io/idta/HierarchicalStructures/EntryNode/1/0_globalAssetId"
                            ]
                        }
                    }
                }
                """;

        const string ResponseJson = """
                {
                    "https://admin-shell.io/idta/CustomSubmodel/Submodel/Template/0/1": {
                        "https://admin-shell.io/idta/HierarchicalStructures/EntryNode/1/0": {
                            "https://admin-shell.io/idta/HierarchicalStructures/EntryNode/1/0_globalAssetId": "https://mm-software.com/ids/assets/000-002"
                        }
                    }
                }
                """;

        var schema = JsonSchema.FromText(SchemaJson);

        _sut.ValidateResponseContent(ResponseJson, schema);
        _sut.ValidateResponseContent(ResponseJson, schema);
    }

    [Fact]
    public void ValidateResponseContent_PropertyTypeStringOrArray_WithArray_DoesNotThrow()
    {
        var schema = new JsonSchemaBuilder()
                     .Type(SchemaValueType.Object)
                     .Properties(new Dictionary<string, JsonSchemaBuilder>
                     {
                         ["value"] = new JsonSchemaBuilder().Type(SchemaValueType.String, SchemaValueType.Array)
                     })
                     .Required("value")
                     .Build();

        const string Json = "{\"value\": [\"one\", \"two\"]}";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Fact]
    public void ValidateResponseContent_PropertyTypeStringOrArray_WithNumber_ThrowsBadRequest()
    {
        var schema = new JsonSchemaBuilder()
                     .Type(SchemaValueType.Object)
                     .Properties(new Dictionary<string, JsonSchemaBuilder>
                     {
                         ["value"] = new JsonSchemaBuilder().Type(SchemaValueType.String, SchemaValueType.Array)
                     })
                     .Required("value")
                     .Build();

        const string Json = "{\"value\": 123}";

        Assert.Throws<NotFoundException>(() => _sut.ValidateResponseContent(Json, schema));
    }

    [Fact]
    public void ValidateResponseContent_SchemaMismatch_ThrowsBadRequest()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["name"] = new JsonSchemaBuilder().Type(SchemaValueType.String)
            })
            .Required("name")
            .Build();

        const string Json = "{}";

        Assert.Throws<NotFoundException>(() => _sut.ValidateResponseContent(Json, schema));
    }

    [Fact]
    public void ValidateResponseContent_WhenSchemaExpectsObjectAndResponseIsArray_ThrowsBadRequest()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Build();

        const string Json = "[]";

        Assert.Throws<NotFoundException>(() => _sut.ValidateResponseContent(Json, schema));
    }

    [Fact]
    public void ValidateResponseContent_InvalidJson_ThrowsBadRequest()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Build();
        const string BadJson = "{ not valid json }";

        Assert.Throws<NotFoundException>(() => _sut.ValidateResponseContent(BadJson, schema));
    }

    [Fact]
    public void ValidateResponseContent_LegacyArraySchema_WithoutItems_DoesNotThrow()
    {
        var malformedSchema = JsonSchema.FromText(
            """
            {
                "$schema": "https://json-schema.org/draft/2020-12/schema",
                "type": "object",
                "properties": {
                    "contactInformation": {
                        "type": "array",
                        "properties": {
                            "name": { "type": "string" }
                        },
                        "required": ["name"]
                    }
                }
            }
            """);
        const string Json = @"{ ""contactInformation"": [{ ""name"": ""test"" }] }";

        _sut.ValidateResponseContent(Json, malformedSchema);
    }

    [Fact]
    public void ValidateResponseContent_CorrectSchema_ArrayWithItems_Succeeds()
    {
        var correctSchema = JsonSchema.FromText(
            """
            {
                "$schema": "https://json-schema.org/draft/2020-12/schema",
                "type": "object",
                "properties": {
                    "contactInformation": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "properties": {
                                "name": { "type": "string" }
                            },
                            "required": ["name"]
                        }
                    }
                }
            }
            """);
        const string ValidJson = @"{ ""contactInformation"": [{ ""name"": ""test"" }] }";

        _sut.ValidateResponseContent(ValidJson, correctSchema);
    }
}

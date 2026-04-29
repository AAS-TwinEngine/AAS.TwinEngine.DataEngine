using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Helper;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Json.Schema;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Providers.PluginDataProvider.Helper;

public class JsonSchemaValidatorTests
{
    private readonly JsonSchemaValidator _sut;

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
        var pluginsConfig = Substitute.For<IOptions<PluginsConfig>>();
        pluginsConfig.Value.Returns(new PluginsConfig
        {
            SubmodelElementIndexContextPrefix = "_aastwinengine_"
        });
        var logger = Substitute.For<ILogger<JsonSchemaValidator>>();
        _sut = new JsonSchemaValidator(pluginsConfig, logger);
    }

    [Fact]
    public void ValidateRequestSchema_NullSchema_ThrowsBadRequest() => Assert.Throws<InternalDataProcessingException>(() => _sut.ValidateRequestSchema(null!));

    [Fact]
    public void ValidateRequestSchema_ValidSchema_DoesNotThrow()
    {
        var schema = new JsonSchemaBuilder()
        .Schema(MetaSchemas.Draft7Id)
        .Type(SchemaValueType.Object)
        .Properties(new Dictionary<string, JsonSchemaBuilder>
        {
            ["name"] = new JsonSchemaBuilder().Type(SchemaValueType.String)
        })
        .Build();

        _sut.ValidateRequestSchema(schema);
    }

    [Fact]
    public void ValidateRequestSchema_ValidDraft202012Schema_DoesNotThrow()
    {
        var schema = new JsonSchemaBuilder()
        .Schema(MetaSchemas.Draft202012Id)
        .Type(SchemaValueType.Object)
        .Properties(new Dictionary<string, JsonSchemaBuilder>
        {
            ["name"] = new JsonSchemaBuilder().Type(SchemaValueType.String)
        })
        .Build();

        _sut.ValidateRequestSchema(schema);
    }

    [Fact]
    public void ValidateRequestSchema_WhenSchemaHasNoSchemaKeyword_DefaultsToDraft202012()
    {
        var schema = BuildDraft202012Schema();

        _sut.ValidateRequestSchema(schema);
    }

    [Fact]
    public void ValidateResponseContent_EmptyResponse_ThrowsBadRequest()
    {
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object).Build();

        Assert.Throws<InternalDataProcessingException>(() => _sut.ValidateResponseContent("", schema));
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
    public void ValidateResponseContent_WhenSchemaDeclaresDraft202012_ValidatesSuccessfully()
    {
        var schema = BuildDraft202012Schema();
        const string Json = "{\"asset\": {\"details\": {\"name\": \"ok\"}}}";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Fact]
    public void ValidateResponseContent_WhenSchemaHasNoSchemaKeyword_DefaultsToDraft202012()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["asset"] = new JsonSchemaBuilder()
                    .Type(SchemaValueType.Object)
                    .Properties(new Dictionary<string, JsonSchemaBuilder>
                    {
                        ["details"] = new JsonSchemaBuilder()
                            .Type(SchemaValueType.Object)
                            .Properties(new Dictionary<string, JsonSchemaBuilder>
                            {
                                ["name"] = new JsonSchemaBuilder().Type(SchemaValueType.String)
                            })
                            .Required("name")
                    })
                    .Required("details")
            })
            .Required("asset")
            .Build();
        const string Json = "{\"asset\": {\"details\": {\"name\": \"ok\"}}}";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Fact]
    public void ValidateResponseContent_WhenSchemaHasNoSchemaKeyword_AndUsesDraft202012Keyword_DefaultsToDraft202012()
    {
        var schema = JsonSchema.FromText(
                """
                        {
                            "type": "object",
                            "properties": {
                                "asset": {
                                    "type": "object",
                                    "properties": {
                                        "name": { "type": "string" }
                                    },
                                    "required": ["name"]
                                }
                            },
                            "required": ["asset"],
                            "unevaluatedProperties": false
                        }
                        """);

        const string Json = "{\"asset\":{\"name\":\"ok\"}}";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Fact]
    public void ValidateResponseContent_WhenSchemaDeclaresDraft7_ValidatesDeepHierarchy()
    {
        var schema = BuildDraft7Schema();
        const string Json = "{\"asset\": {\"details\": {\"name\": \"Motor\", \"tags\": [\"a\",\"b\"]}}}";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Fact]
    public void ValidateResponseContent_WhenRequiredNestedPropertyMissing_ThrowsBadRequest()
    {
        var schema = BuildDraft202012Schema();
        const string Json = "{\"asset\": {\"details\": {}}}";

        Assert.Throws<InternalDataProcessingException>(() =>
            _sut.ValidateResponseContent(Json, schema));
    }

    [Fact]
    public void ValidateResponseContent_WhenAdditionalPropertiesNotAllowed_ThrowsBadRequest()
    {
        var schema = BuildDraft7Schema();
        const string Json = "{\"asset\": {\"details\": {\"name\": \"Motor\", \"unexpected\": 1}}}";

        Assert.Throws<InternalDataProcessingException>(() =>
            _sut.ValidateResponseContent(Json, schema));
    }

    [Fact]
    public void ValidateResponseContent_WhenUnevaluatedPropertiesNotAllowed_ThrowsBadRequest()
    {
        var schema = BuildDraft202012Schema();
        const string Json = "{\"asset\": {\"details\": {\"name\": \"Motor\"}}, \"unexpected\": 1}";

        Assert.Throws<InternalDataProcessingException>(() =>
            _sut.ValidateResponseContent(Json, schema));
    }

    [Fact]
    public void ValidateResponseContent_WhenNullAtDeepLevel_ThrowsBadRequest()
    {
        var schema = BuildDraft202012Schema();
        const string Json = "{\"asset\": {\"details\": {\"name\": null}}}";

        Assert.Throws<InternalDataProcessingException>(() =>
            _sut.ValidateResponseContent(Json, schema));
    }

    [Fact]
    public void ValidateResponseContent_WhenPartialPayloadMissingRequired_ThrowsBadRequest()
    {
        var schema = BuildDraft7Schema();
        const string Json = "{\"asset\": {}}";

        Assert.Throws<InternalDataProcessingException>(() =>
                        _sut.ValidateResponseContent(Json, schema));
    }

    [Fact]
    public void ValidateResponseContent_WhenDraft7SchemaContainsDraft202012Construct_ThrowsBadRequest()
    {
        var schema = JsonSchema.FromText(
                """
                        {
                            "$schema": "http://json-schema.org/draft-07/schema#",
                            "type": "object",
                            "properties": {
                                "name": { "type": "string" }
                            },
                            "required": ["name"],
                            "unevaluatedProperties": false
                        }
                        """);
        const string Json = "{\"asset\": {\"details\": {\"name\": \"ok\"}}}";

        Assert.Throws<InternalDataProcessingException>(() =>
                        _sut.ValidateResponseContent(Json, schema));
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

        Assert.Throws<InternalDataProcessingException>(() => _sut.ValidateResponseContent(json, schema));
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

        Assert.Throws<InternalDataProcessingException>(() => _sut.ValidateResponseContent(Json, schema));
    }

    [Fact]
    public void ValidateResponseContent_WhenSchemaExpectsObjectAndResponseIsArray_ThrowsBadRequest()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Build();

        const string Json = "[]";

        Assert.Throws<InternalDataProcessingException>(() => _sut.ValidateResponseContent(Json, schema));
    }

    [Fact]
    public void ValidateResponseContent_InvalidJson_ThrowsBadRequest()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Build();
        const string BadJson = "{ not valid json }";

        Assert.Throws<InternalDataProcessingException>(() => _sut.ValidateResponseContent(BadJson, schema));
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

    private static JsonSchema BuildDraft7Schema() => JsonSchema.FromText(
            """
                {
                    "$schema": "http://json-schema.org/draft-07/schema#",
                    "type": "object",
                    "properties": {
                        "asset": {
                            "type": "object",
                            "properties": {
                                "details": {
                                    "type": "object",
                                    "properties": {
                                        "name": { "type": "string" },
                                        "tags": {
                                            "type": "array",
                                            "items": { "type": "string" }
                                        }
                                    },
                                    "required": ["name"],
                                    "additionalProperties": false
                                }
                            },
                            "required": ["details"],
                            "additionalProperties": false
                        }
                    },
                    "required": ["asset"],
                    "additionalProperties": false
                }
                """);

    private static JsonSchema BuildDraft202012Schema() => JsonSchema.FromText(
            """
                {
                    "$schema": "https://json-schema.org/draft/2020-12/schema",
                    "type": "object",
                    "properties": {
                        "asset": {
                            "type": "object",
                            "properties": {
                                "details": {
                                    "type": "object",
                                    "properties": {
                                        "name": { "type": "string" },
                                        "tags": {
                                            "type": "array",
                                            "items": { "type": "string" }
                                        }
                                    },
                                    "required": ["name"],
                                    "unevaluatedProperties": false
                                }
                            },
                            "required": ["details"],
                            "unevaluatedProperties": false
                        }
                    },
                    "required": ["asset"],
                    "unevaluatedProperties": false
                }
                """);
}

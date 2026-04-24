using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;
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
    public void ValidateRequestSchema_NullSchema_ThrowsBadRequest()
        => Assert.Throws<InternalDataProcessingException>(() => _sut.ValidateRequestSchema(null!));

    [Fact]
    public void ValidateRequestSchema_ValidSchema_DoesNotThrow()
    {
        var schema = new JsonSchemaBuilder()
            .Schema("https://json-schema.org/draft/2020-12/schema")
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["name"] = new JsonSchemaBuilder().Type(SchemaValueType.String)
            })
            .Build();

        _sut.ValidateRequestSchema(schema);
    }

    [Fact]
    public void ValidateResponseContent_EmptyResponse_ThrowsBadRequest()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Build();

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
    public void ValidateResponseContent_InvalidJson_ThrowsBadRequest()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Build();

        const string BadJson = "{ not valid json }";

        Assert.Throws<InternalDataProcessingException>(() => _sut.ValidateResponseContent(BadJson, schema));
    }

    [Fact]
    public void ValidateResponseContent_NullResponse_ThrowsException()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Build();

        Assert.Throws<InternalDataProcessingException>(() => _sut.ValidateResponseContent(null!, schema));
    }

    [Fact]
    public void ValidateResponseContent_WhitespaceOnlyResponse_ThrowsException()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Build();

        Assert.Throws<InternalDataProcessingException>(() => _sut.ValidateResponseContent("   ", schema));
    }

    [Fact]
    public void ValidateResponseContent_MalformedJson_ThrowsException()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Build();

        Assert.Throws<InternalDataProcessingException>(() => _sut.ValidateResponseContent("{\"key\": }", schema));
    }
    [Fact]
    public void ValidateResponseContent_ArrayOfObjects_ValidatesCorrectly()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["users"] = new JsonSchemaBuilder()
                    .Type(SchemaValueType.Array)
                    .Items(new JsonSchemaBuilder()
                        .Type(SchemaValueType.Object)
                        .Properties(new Dictionary<string, JsonSchemaBuilder>
                        {
                            ["name_aastwinengine_01"] = new JsonSchemaBuilder().Type(SchemaValueType.String)
                        }))
            })
            .Build();

        const string Json = "{\"users\": [{\"name\": \"Alice\"}, {\"name\": \"Bob\"}]}";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Fact]
    public void ValidateResponseContent_WithDefsReference_DoesNotThrow()
    {
        const string schemaJson = @"{
        ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
        ""type"": ""object"",
        ""properties"": {
            ""item"": { ""$ref"": ""#/$defs/MyType"" }
        },
        ""$defs"": {
            ""MyType"": {
                ""type"": ""object"",
                ""properties"": {
                    ""name"": { ""type"": ""string"" }
                }
            }
        }
        }";

        var schema = JsonSchema.FromText(schemaJson);

        const string json = @"{ ""item"": { ""name"": ""test"" } }";

        _sut.ValidateResponseContent(json, schema);
    }

    [Fact]
    public void ValidateResponseContent_BrokenRef_Throws()
    {
        const string schemaJson = @"{
        ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
        ""type"": ""object"",
        ""properties"": {
            ""item"": { ""$ref"": ""#/$defs/UnknownType"" }
        },
        ""$defs"": {}
        }";

        var schema = JsonSchema.FromText(schemaJson);

        const string json = @"{ ""item"": {} }";

        Assert.Throws<InternalDataProcessingException>(() =>
            _sut.ValidateResponseContent(json, schema));
    }

    [Fact]
    public void ValidateResponseContent_ArrayItemInvalid_Throws()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["users"] = new JsonSchemaBuilder()
                    .Type(SchemaValueType.Array)
                    .Items(new JsonSchemaBuilder()
                        .Type(SchemaValueType.Object)
                        .Properties(new Dictionary<string, JsonSchemaBuilder>
                        {
                            ["name"] = new JsonSchemaBuilder().Type(SchemaValueType.String)
                        })
                        .Required("name"))
            })
            .Build();

        const string json = @"{ ""users"": [{}] }";

        Assert.Throws<InternalDataProcessingException>(() =>
            _sut.ValidateResponseContent(json, schema));
    }

    [Fact]
    public void ValidateResponseContent_NestedDefsReference_Works()
    {
        const string schemaJson = @"{
        ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
        ""type"": ""object"",
        ""properties"": {
            ""item"": { ""$ref"": ""#/$defs/Level1"" }
        },
        ""$defs"": {
            ""Level1"": {
                ""type"": ""object"",
                ""properties"": {
                    ""child"": { ""$ref"": ""#/$defs/Level2"" }
                }
            },
            ""Level2"": {
                ""type"": ""string""
            }
            }
        }";
        var schema = JsonSchema.FromText(schemaJson);

        const string json = @"{ ""item"": { ""child"": ""ok"" } }";

        _sut.ValidateResponseContent(json, schema);
    }

    [Fact]
    public void ValidateResponseContent_ArrayMissingRequiredField_Throws()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["items"] = new JsonSchemaBuilder()
                    .Type(SchemaValueType.Array)
                    .Items(new JsonSchemaBuilder()
                        .Type(SchemaValueType.Object)
                        .Properties(new Dictionary<string, JsonSchemaBuilder>
                        {
                            ["name"] = new JsonSchemaBuilder().Type(SchemaValueType.String)
                        })
                        .Required("name"))
            })
            .Build();

        const string json = @"{ ""items"": [{}] }";

        Assert.Throws<InternalDataProcessingException>(() =>
            _sut.ValidateResponseContent(json, schema));
    }
}

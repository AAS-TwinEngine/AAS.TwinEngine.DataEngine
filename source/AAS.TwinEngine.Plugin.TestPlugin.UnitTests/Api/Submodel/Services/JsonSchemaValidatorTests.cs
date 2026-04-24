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
        var logger = Substitute.For<ILogger<JsonSchemaValidator>>();
        _sut = new JsonSchemaValidator(semantics, logger);
    }

    [Fact]
    public void ValidateResponseContent_EmptyResponse_ThrowsBadRequest()
    {
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object).Build();

        Assert.Throws<NotFoundException>(() => _sut.ValidateResponseContent("", schema));
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

        Assert.Throws<NotFoundException>(() => _sut.ValidateResponseContent(json, schema));
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
    public void ValidateResponseContent_InvalidJson_ThrowsBadRequest()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Build();

        const string BadJson = "{ not valid json }";

        Assert.Throws<NotFoundException>(() => _sut.ValidateResponseContent(BadJson, schema));
    }

    [Fact]
    public void ValidateResponseContent_NullResponse_ThrowsException()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Build();

        Assert.Throws<NotFoundException>(() => _sut.ValidateResponseContent(null!, schema));
    }

    [Fact]
    public void ValidateResponseContent_WhitespaceOnlyResponse_ThrowsException()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Build();

        Assert.Throws<NotFoundException>(() => _sut.ValidateResponseContent("   ", schema));
    }

    [Fact]
    public void ValidateResponseContent_MalformedJson_ThrowsException()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Build();

        Assert.Throws<NotFoundException>(() => _sut.ValidateResponseContent("{\"key\": }", schema));
    }

    [Fact]
    public void ValidateResponseContent_PropertyWithSuffix_RemovesSuffix()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["PropertyName_aastwinengine_123"] = new JsonSchemaBuilder().Type(SchemaValueType.String)
            })
            .Required("PropertyName_aastwinengine_123")
            .Build();

        const string Json = "{\"PropertyName\": \"value\"}";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Fact]
    public void ValidateResponseContent_PropertyWithoutSuffix_DoesNotModify()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["PropertyWithoutSuffix"] = new JsonSchemaBuilder().Type(SchemaValueType.String)
            })
            .Required("PropertyWithoutSuffix")
            .Build();

        const string Json = "{\"PropertyWithoutSuffix\": \"value\"}";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Fact]
    public void ValidateResponseContent_NestedObjectWithSuffixedProperties_RemovesSuffixes()
    {
        var nestedSchema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["nestedField_aastwinengine_01"] = new JsonSchemaBuilder().Type(SchemaValueType.String)
            });

        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["parent_aastwinengine_00"] = nestedSchema
            })
            .Build();

        const string Json = "{\"parent\": {\"nestedField\": \"value\"}}";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Fact]
    public void ValidateResponseContent_ArrayWithSuffixedItems_RemovesSuffixes()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["items_aastwinengine_01"] = new JsonSchemaBuilder()
                    .Type(SchemaValueType.Array)
                    .Items(new JsonSchemaBuilder().Type(SchemaValueType.String))
            })
            .Build();

        const string Json = "{\"items\": [\"item1\", \"item2\"]}";

        _sut.ValidateResponseContent(Json, schema);
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
    public void ValidateResponseContent_EmptyArray_ValidatesCorrectly()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["items"] = new JsonSchemaBuilder()
                    .Type(SchemaValueType.Array)
                    .Items(new JsonSchemaBuilder().Type(SchemaValueType.String))
            })
            .Build();

        const string Json = "{\"items\": []}";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Fact]
    public void ValidateResponseContent_RequiredPropertyWithSuffix_ValidatesAfterRemoval()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["requiredField_aastwinengine_01"] = new JsonSchemaBuilder().Type(SchemaValueType.String)
            })
            .Required("requiredField_aastwinengine_01")
            .Build();

        const string Json = "{\"requiredField\": \"value\"}";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Fact]
    public void ValidateResponseContent_MissingRequiredProperty_ThrowsException()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["requiredField_aastwinengine_01"] = new JsonSchemaBuilder().Type(SchemaValueType.String)
            })
            .Required("requiredField_aastwinengine_01")
            .Build();

        const string Json = "{}";

        Assert.Throws<NotFoundException>(() => _sut.ValidateResponseContent(Json, schema));
    }

    [Fact]
    public void ValidateResponseContent_SchemaWithoutId_ValidatesCorrectly()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["field"] = new JsonSchemaBuilder().Type(SchemaValueType.String)
            })
            .Build();

        const string Json = "{\"field\": \"value\"}";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Fact]
    public void ValidateResponseContent_PropertyNameWithHyphen_HandlesCorrectly()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["field-name_aastwinengine_01"] = new JsonSchemaBuilder().Type(SchemaValueType.String)
            })
            .Build();

        const string Json = "{\"field-name\": \"value\"}";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Fact]
    public void ValidateResponseContent_PropertyNameWithDot_HandlesCorrectly()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["field.name_aastwinengine_01"] = new JsonSchemaBuilder().Type(SchemaValueType.String)
            })
            .Build();

        const string Json = "{\"field.name\": \"value\"}";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Fact]
    public void ValidateResponseContent_PropertyNameWithUnicode_HandlesCorrectly()
    {
        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["field名前_aastwinengine_01"] = new JsonSchemaBuilder().Type(SchemaValueType.String)
            })
            .Build();

        const string Json = "{\"field名前\": \"value\"}";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Fact]
    public void ValidateResponseContent_DeeplyNestedStructure_ValidatesCorrectly()
    {
        var level3 = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["deepField_aastwinengine_03"] = new JsonSchemaBuilder().Type(SchemaValueType.String)
            });

        var level2 = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["midField_aastwinengine_02"] = level3
            });

        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["topField_aastwinengine_01"] = level2
            })
            .Build();

        const string Json = "{\"topField\": {\"midField\": {\"deepField\": \"value\"}}}";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Fact]
    public void ValidateResponseContent_MixedArrayAndObjectNesting_ValidatesCorrectly()
    {
        var objectSchema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["items_aastwinengine_02"] = new JsonSchemaBuilder()
                    .Type(SchemaValueType.Array)
                    .Items(new JsonSchemaBuilder().Type(SchemaValueType.String))
            });

        var schema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["data_aastwinengine_01"] = new JsonSchemaBuilder()
                    .Type(SchemaValueType.Array)
                    .Items(objectSchema)
            })
            .Build();

        const string Json = "{\"data\": [{\"items\": [\"a\", \"b\"]}, {\"items\": [\"c\"]}]}";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Fact]
    public void ValidateResponseContent_WithDefsReference_DoesNotThrow()
    {
        const string schemaJson = @"{
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
}

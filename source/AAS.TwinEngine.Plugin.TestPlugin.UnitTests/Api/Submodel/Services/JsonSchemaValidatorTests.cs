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
        .Properties(new Dictionary<string, JsonSchema>
        {
            ["ContactInformation_aastwinengine_00"] = new JsonSchemaBuilder().Type(SchemaValueType.Object).Build()
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
        .Properties(new Dictionary<string, JsonSchema>
        {
            ["name"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build()
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
            .Properties(new Dictionary<string, JsonSchema>
            {
                [property] = new JsonSchemaBuilder().Type(expectedType).Build()
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
                     .Properties(new Dictionary<string, JsonSchema>
                     {
                         ["value"] = new JsonSchemaBuilder().Type(SchemaValueType.String, SchemaValueType.Array).Build()
                     })
                     .Required("value")
                     .Build();

        const string Json = "{\"value\": \"hello\"}";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Fact]
    public void ValidateResponseContent_PropertyTypeStringOrArray_WithArray_DoesNotThrow()
    {
        var schema = new JsonSchemaBuilder()
                     .Type(SchemaValueType.Object)
                     .Properties(new Dictionary<string, JsonSchema>
                     {
                         ["value"] = new JsonSchemaBuilder().Type(SchemaValueType.String, SchemaValueType.Array).Build()
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
                     .Properties(new Dictionary<string, JsonSchema>
                     {
                         ["value"] = new JsonSchemaBuilder().Type(SchemaValueType.String, SchemaValueType.Array).Build()
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
            .Properties(new Dictionary<string, JsonSchema>
            {
                ["name"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build()
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
}

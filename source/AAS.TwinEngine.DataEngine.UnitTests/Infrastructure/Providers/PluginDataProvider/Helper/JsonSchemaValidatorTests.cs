using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Helper;

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
        var semantics = Substitute.For<IOptions<Semantics>>();
        semantics.Value.Returns(new Semantics
        {
            SubmodelElementIndexContextPrefix = "_aastwinengine_"
        });
        var logger = Substitute.For<ILogger<JsonSchemaValidator>>();
        _sut = new JsonSchemaValidator(semantics, logger);
    }

    [Fact]
    public void ValidateRequestSchema_NullSchema_ThrowsBadRequest() => Assert.Throws<InternalDataProcessingException>(() => _sut.ValidateRequestSchema(null!));

    [Fact]
    public void ValidateRequestSchema_WithInvalidJson_ThrowsParseError()
    {
        var schema = new JsonSchemaBuilder()
        .Type(SchemaValueType.Object)
        .Properties(new Dictionary<string, JsonSchema>
        {
            ["invalid"] = null!
        })
        .Build();

        Assert.Throws<InternalDataProcessingException>(() => _sut.ValidateRequestSchema(schema));
    }

    [Fact]
    public void ValidateRequestSchema_ValidSchema_DoesNotThrow()
    {
        var schema = new JsonSchemaBuilder()
        .Schema("http://json-schema.org/draft-07/schema#")
        .Type(SchemaValueType.Object)
        .Properties(new Dictionary<string, JsonSchema>
        {
            ["name"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build()
        })
        .Build();

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

        Assert.Throws<InternalDataProcessingException>(() => _sut.ValidateResponseContent(json, schema));
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
}

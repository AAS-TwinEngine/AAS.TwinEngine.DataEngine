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
    private readonly ILogger<JsonSchemaSecurityValidator> _securityLogger;
    private readonly JsonSchemaSecurityValidator _securityValidator;

    public static IEnumerable<object[]> InvalidPrimitives => [
        [SchemaValueType.String,  "name",  123],
        [SchemaValueType.Integer, "count", 12.34],
        [SchemaValueType.Number,  "price", "19.99a"],
        [SchemaValueType.Boolean, "flag",  "flase"],
        [SchemaValueType.Number,  "age",   "8o5"],
        [SchemaValueType.Number,  "age", "-10n5"],
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
        _securityLogger = Substitute.For<ILogger<JsonSchemaSecurityValidator>>();
        _securityValidator = new JsonSchemaSecurityValidator(semantics, _securityLogger);
        _sut = new JsonSchemaValidator(semantics, _logger, _securityValidator);
    }

    [Fact]
    public void ValidateRequestSchema_NullSchema_ThrowsBadRequestException()
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
    public void ValidateRequestSchema_WithInvalidJson_ThrowsBadRequestException()
    {
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object)
                                            .Properties(new Dictionary<string, JsonSchema>
                                            {
                                                ["invalid"] = null!
                                            })
                                            .Build();

        Assert.Throws<BadRequestException>(() => _sut.ValidateRequestSchema(schema));
    }

    [Fact]
    public void ValidateRequestSchema_ValidSchema_DoesNotThrow()
    {
        var schema = new JsonSchemaBuilder().Schema("http://json-schema.org/draft-07/schema#")
                                            .Type(SchemaValueType.Object)
                                            .Properties(new Dictionary<string, JsonSchema>
                                            {
                                                ["name"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build()
                                            })
                                            .Build();

        _sut.ValidateRequestSchema(schema);
    }

    [Fact]
    public void ValidateRequestSchema_EmptySchema_DoesNotThrow()
    {
        var schema = new JsonSchemaBuilder()
                     .Type(SchemaValueType.Object)
                     .Build();

        _sut.ValidateRequestSchema(schema);
    }

    [Fact]
    public void ValidateRequestSchema_ComplexNestedSchema_DoesNotThrow()
    {
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object)
                                            .Properties(new Dictionary<string, JsonSchema>
                                            {
                                                ["person"] = new JsonSchemaBuilder()
                                                             .Type(SchemaValueType.Object)
                                                             .Properties(new Dictionary<string, JsonSchema>
                                                             {
                                                                 ["name"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build(),
                                                                 ["age"] = new JsonSchemaBuilder().Type(SchemaValueType.Integer).Build()
                                                             })
                                                             .Build()
                                            })
                                            .Build();

        _sut.ValidateRequestSchema(schema);
    }

    [Fact]
    public void ValidateRequestSchema_SchemaWithDefinitions_DoesNotThrow()
    {
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object)
                                            .Defs(new Dictionary<string, JsonSchema>
                                            {
                                                ["address"] = new JsonSchemaBuilder()
                                                              .Type(SchemaValueType.Object)
                                                              .Properties(new Dictionary<string, JsonSchema>
                                                              {
                                                                  ["street"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build()
                                                              })
                                                              .Build()
                                            })
                                            .Build();

        _sut.ValidateRequestSchema(schema);
    }

    [Fact]
    public void ValidateResponseContent_EmptyResponse_ThrowsNotFoundException()
    {
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object).Build();

        Assert.Throws<NotFoundException>(() => _sut.ValidateResponseContent("", schema));
    }

    [Fact]
    public void ValidateResponseContent_NullResponse_ThrowsNotFoundException()
    {
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object).Build();

        Assert.Throws<NotFoundException>(() => _sut.ValidateResponseContent(null!, schema));
    }

    [Fact]
    public void ValidateResponseContent_WhitespaceResponse_ThrowsNotFoundException()
    {
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object).Build();

        Assert.Throws<NotFoundException>(() => _sut.ValidateResponseContent("   ", schema));
    }

    [Fact]
    public void ValidateResponseContent_ValidateJsonSchemaRemovePrefix_DoesNotThrow()
    {
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object)
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
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object)
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
    public void ValidateResponseContent_InvalidValueType_ThrowsNotFoundException(SchemaValueType expectedType,
                                                                                 string property,
                                                                                 object rawValue)
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
    public void ValidateResponseContent_SchemaMismatch_ThrowsBadRequestException()
    {
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object)
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
    public void ValidateResponseContent_InvalidJson_ThrowsNotFoundException()
    {
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object)
                                            .Build();

        const string BadJson = "{ not valid json }";

        Assert.Throws<NotFoundException>(() => _sut.ValidateResponseContent(BadJson, schema));
    }

    [Fact]
    public void ValidateResponseContent_MalformedJson_ThrowsNotFoundException()
    {
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object).Build();

        const string MalformedJson = "{\"key\": }";

        Assert.Throws<NotFoundException>(() => _sut.ValidateResponseContent(MalformedJson, schema));
    }

    [Fact]
    public void ValidateResponseContent_JsonWithExtraProperties_DoesNotThrow()
    {
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object)
                                            .Properties(new Dictionary<string, JsonSchema>
                                            {
                                                ["name"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build()
                                            })
                                            .Required("name")
                                            .Build();

        const string Json = "{\"name\": \"Test\", \"extra\": \"value\"} ";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Fact]
    public void ValidateResponseContent_NestedObjectValidation_DoesNotThrow()
    {
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object)
                                            .Properties(new Dictionary<string, JsonSchema>
                                            {
                                                ["person"] = new JsonSchemaBuilder()
                                                             .Type(SchemaValueType.Object)
                                                             .Properties(new Dictionary<string, JsonSchema>
                                                             {
                                                                 ["name"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build(),
                                                                 ["age"] = new JsonSchemaBuilder().Type(SchemaValueType.Integer).Build()
                                                             })
                                                             .Required("name", "age")
                                                             .Build()
                                            })
                                            .Required("person")
                                            .Build();

        const string Json = "{\"person\": {\"name\": \"John\", \"age\": 30}}";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Fact]
    public void ValidateResponseContent_ArrayValidation_DoesNotThrow()
    {
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object)
                                            .Properties(new Dictionary<string, JsonSchema>
                                            {
                                                ["items"] = new JsonSchemaBuilder()
                                                            .Type(SchemaValueType.Array)
                                                            .Items(new JsonSchemaBuilder().Type(SchemaValueType.String).Build())
                                                            .Build()
                                            })
                                            .Required("items")
                                            .Build();

        const string Json = "{\"items\": [\"item1\", \"item2\", \"item3\"]} ";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Fact]
    public void ValidateResponseContent_WithContextPrefixInMultipleProperties_DoesNotThrow()
    {
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object)
                                            .Properties(new Dictionary<string, JsonSchema>
                                            {
                                                ["Property1_aastwinengine_00"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build(),
                                                ["Property2_aastwinengine_01"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build()
                                            })
                                            .Required("Property1_aastwinengine_00", "Property2_aastwinengine_01")
                                            .Build();

        const string Json = "{\"Property1\": \"value1\", \"Property2\": \"value2\"} ";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Fact]
    public void ValidateResponseContent_ComplexSchemaWithNestedArrays_DoesNotThrow()
    {
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object)
                                            .Properties(new Dictionary<string, JsonSchema>
                                            {
                                                ["users"] = new JsonSchemaBuilder()
                                                            .Type(SchemaValueType.Array)
                                                            .Items(new JsonSchemaBuilder()
                                                                   .Type(SchemaValueType.Object)
                                                                   .Properties(new Dictionary<string, JsonSchema>
                                                                   {
                                                                       ["name"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build(),
                                                                       ["emails"] = new JsonSchemaBuilder()
                                                                                    .Type(SchemaValueType.Array)
                                                                                    .Items(new JsonSchemaBuilder().Type(SchemaValueType.String).Build())
                                                                                    .Build()
                                                                   })
                                                                   .Build())
                                                            .Build()
                                            })
                                            .Build();

        const string Json = "{\"users\": [{\"name\": \"Alice\", \"emails\": [\"alice@test.com\"]}, {\"name\": \"Bob\", \"emails\": [\"bob@test.com\"]}]} ";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Fact]
    public void ValidateResponseContent_WithSpecialCharactersInPropertyNames_DoesNotThrow()
    {
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object)
                                            .Properties(new Dictionary<string, JsonSchema>
                                            {
                                                ["property/with~special_aastwinengine_00"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build()
                                            })
                                            .Required("property/with~special_aastwinengine_00")
                                            .Build();

        const string Json = "{\"property/with~special\": \"value\"} ";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Fact]
    public void ValidateResponseContent_EmptyArray_DoesNotThrow()
    {
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object)
                                            .Properties(new Dictionary<string, JsonSchema>
                                            {
                                                ["items"] = new JsonSchemaBuilder()
                                                            .Type(SchemaValueType.Array)
                                                            .Items(new JsonSchemaBuilder().Type(SchemaValueType.String).Build())
                                                            .Build()
                                            })
                                            .Required("items")
                                            .Build();

        const string Json = "{\"items\": []} ";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Fact]
    public void ValidateResponseContent_LogsError_WhenValidationFails()
    {
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object)
                                            .Properties(new Dictionary<string, JsonSchema>
                                            {
                                                ["name"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build()
                                            })
                                            .Required("name")
                                            .Build();

        const string Json = "{}";

        Assert.Throws<NotFoundException>(() => _sut.ValidateResponseContent(Json, schema));

        _logger.Received(2).Log(LogLevel.Error,
                                Arg.Any<EventId>(),
                                Arg.Any<object>(),
                                Arg.Any<Exception>(),
                                Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void ValidateResponseContent_WithNullableProperties_DoesNotThrow()
    {
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object)
                                            .Properties(new Dictionary<string, JsonSchema>
                                            {
                                                ["optionalField"] = new JsonSchemaBuilder().Type(SchemaValueType.String, SchemaValueType.Null).Build()
                                            })
                                            .Build();

        const string Json = "{\"optionalField\": null}";

        _sut.ValidateResponseContent(Json, schema);
    }

    [Fact]
    public void ValidateRequestSchema_ExceedsMaxSchemaSize_ThrowsBadRequestException()
    {
        var properties = new Dictionary<string, JsonSchema>();
        for (var i = 0; i < 15000; i++)
        {
            properties[$"property_{i:D5}_with_a_long_name_to_increase_size"] =
                 new JsonSchemaBuilder().Type(SchemaValueType.String).Build();
        }

        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object)
                                            .Properties(properties)
                                            .Build();

        Assert.Throws<BadRequestException>(() => _sut.ValidateRequestSchema(schema));
    }

    [Fact]
    public void ValidateRequestSchema_ExceedsMaxSchemaDepth_ThrowsBadRequestException()
    {
        var schema = BuildNestedSchema(12); // Depth of 12 exceeds limit of 10

        var exception = Assert.Throws<BadRequestException>(() => _sut.ValidateRequestSchema(schema));
        Assert.Contains("Schema nesting too deep", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Maximum allowed depth is 10", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateRequestSchema_ExceedsMaxProperties_ThrowsBadRequestException()
    {
        var properties = new Dictionary<string, JsonSchema>();

        for (var i = 0; i < 1100; i++)
        {
            properties[$"prop{i}"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build();
        }

        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object)
                                            .Properties(properties)
                                            .Build();

        var exception = Assert.Throws<BadRequestException>(() => _sut.ValidateRequestSchema(schema));
        Assert.Contains("Schema contains too many properties", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Maximum allowed is 1000", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateRequestSchema_AtMaxProperties_DoesNotThrow()
    {
        var properties = new Dictionary<string, JsonSchema>();

        for (var i = 0; i < 1000; i++)
        {
            properties[$"property{i}"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build();
        }

        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object)
                                            .Properties(properties)
                                            .Build();

        _sut.ValidateRequestSchema(schema);
    }

    [Fact]
    public void ValidateRequestSchema_MultipleNestedObjectsWithManyProperties_ExceedsLimit()
    {
        var nestedProperties = new Dictionary<string, JsonSchema>();
        for (var i = 0; i < 600; i++)
        {
            nestedProperties[$"nested{i}"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build();
        }

        var properties = new Dictionary<string, JsonSchema>();
        for (var i = 0; i < 500; i++)
        {
            properties[$"prop{i}"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build();
        }

        properties["nestedObject"] = new JsonSchemaBuilder().Type(SchemaValueType.Object)
                                                            .Properties(nestedProperties)
                                                            .Build();
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object)
                                            .Properties(properties)
                                            .Build();

        var exception = Assert.Throws<BadRequestException>(() => _sut.ValidateRequestSchema(schema));
        Assert.Contains("Schema contains too many properties", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateRequestSchema_ArrayWithNestedObjects_CountsPropertiesCorrectly()
    {
        var itemProperties = new Dictionary<string, JsonSchema>();
        for (var i = 0; i < 600; i++)
        {
            itemProperties[$"field{i}"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build();
        }

        var rootProperties = new Dictionary<string, JsonSchema>();
        for (var i = 0; i < 500; i++)
        {
            rootProperties[$"root{i}"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build();
        }

        rootProperties["items"] = new JsonSchemaBuilder().Type(SchemaValueType.Array)
                                                         .Items(new JsonSchemaBuilder()
                                                                .Type(SchemaValueType.Object)
                                                                .Properties(itemProperties)
                                                                .Build())
                                                         .Build();

        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object)
                                            .Properties(rootProperties)
                                            .Build();

        var exception = Assert.Throws<BadRequestException>(() => _sut.ValidateRequestSchema(schema));
        Assert.Contains("Schema contains too many properties", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateRequestSchema_DeepNestingWithArrays_ThrowsBadRequestException()
    {
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object)
                                            .Properties(new Dictionary<string, JsonSchema>
                                            {
                                                ["root"] = BuildDeepArrayStructure(11)
                                            })
                                            .Build();

        var exception = Assert.Throws<BadRequestException>(() => _sut.ValidateRequestSchema(schema));
        Assert.Contains("Schema nesting too deep", exception.Message, StringComparison.CurrentCulture);
    }

    [Fact]
    public void ValidateRequestSchema_ComplexSchemaWithinLimits_DoesNotThrow()
    {
        var level2Properties = new Dictionary<string, JsonSchema>();
        for (var i = 0; i < 100; i++)
        {
            level2Properties[$"l2_{i}"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build();
        }

        var level1Properties = new Dictionary<string, JsonSchema>();
        for (var i = 0; i < 100; i++)
        {
            level1Properties[$"l1_{i}"] = new JsonSchemaBuilder().Type(SchemaValueType.Integer).Build();
        }

        level1Properties["nested"] = new JsonSchemaBuilder().Type(SchemaValueType.Object)
                                                            .Properties(level2Properties)
                                                            .Build();

        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object)
                                            .Properties(level1Properties)
                                            .Build();

        _sut.ValidateRequestSchema(schema);
    }

    [Fact]
    public void ValidateRequestSchema_EmptyPropertiesObject_DoesNotCountAsProperty()
    {
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object)
                                            .Properties(new Dictionary<string, JsonSchema>())
                                            .Build();

        _sut.ValidateRequestSchema(schema);
    }

    [Fact]
    public void ValidateRequestSchema_BoundaryDepthOf11_ThrowsBadRequestException()
    {
        var schema = BuildNestedSchema(11);

        var exception = Assert.Throws<BadRequestException>(() => _sut.ValidateRequestSchema(schema));
        Assert.Contains("Schema nesting too deep", exception.Message, StringComparison.CurrentCulture);
    }

    [Fact]
    public void ValidateRequestSchema_BoundaryPropertiesCount1001_ThrowsBadRequestException()
    {
        var properties = new Dictionary<string, JsonSchema>();

        for (var i = 0; i < 1001; i++)
        {
            properties[$"p{i}"] = new JsonSchemaBuilder().Type(SchemaValueType.Boolean).Build();
        }

        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object)
                                            .Properties(properties)
                                            .Build();

        var exception = Assert.Throws<BadRequestException>(() => _sut.ValidateRequestSchema(schema));
        Assert.Contains("Maximum allowed is 1000", exception.Message, StringComparison.CurrentCulture);
    }

    [Fact]
    public void ValidateRequestSchema_PropertyNameExceedsMaxLength_ThrowsBadRequestException()
    {
        var longPropertyName = new string('a', 300); // Exceeds 256 character limit
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object)
                                            .Properties(new Dictionary<string, JsonSchema>
                                            {
                                                [longPropertyName] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build()
                                            })
                                            .Build();

        Assert.Throws<BadRequestException>(() => _sut.ValidateRequestSchema(schema));
    }

    [Theory]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("'; DROP TABLE Users; --")]
    [InlineData("../../etc/passwd")]
    [InlineData("javascript:alert(1)")]
    [InlineData("<img onerror='alert(1)'>")]
    public void ValidateRequestSchema_PropertyNameWithMaliciousPatterns_ThrowsBadRequestException(string maliciousName)
    {
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object)
                                            .Properties(new Dictionary<string, JsonSchema>
                                            {
                                                [maliciousName] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build()
                                            })
                                            .Build();

        Assert.Throws<BadRequestException>(() => _sut.ValidateRequestSchema(schema));
    }

    [Fact]
    public void ValidateRequestSchema_StringValueExceedsMaxLength_ThrowsBadRequestException()
    {
        var longString = new string('a', 3000); // Exceeds 2048 character limit
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object)
                                            .Properties(new Dictionary<string, JsonSchema>
                                            {
                                                ["description"] = new JsonSchemaBuilder()
                                                                  .Type(SchemaValueType.String)
                                                                  .Const(longString)
                                                                  .Build()
                                            })
                                            .Build();

        Assert.Throws<BadRequestException>(() => _sut.ValidateRequestSchema(schema));
    }

    [Theory]
    [InlineData("test\0value")]
    [InlineData("test%00value")]
    public void ValidateRequestSchema_StringValueWithNullBytes_ThrowsBadRequestException(string valueWithNullByte)
    {
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object)
                                            .Properties(new Dictionary<string, JsonSchema>
                                            {
                                                ["field"] = new JsonSchemaBuilder()
                                                            .Type(SchemaValueType.String)
                                                            .Const(valueWithNullByte)
                                                            .Build()
                                            })
                                            .Build();

        Assert.Throws<BadRequestException>(() => _sut.ValidateRequestSchema(schema));
    }

    [Theory]
    [InlineData("ftp://example.com/schema")]
    [InlineData("file:///etc/passwd")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    public void ValidateRequestSchema_InvalidUriScheme_ThrowsBadRequestException(string invalidUri)
    {
        var schema = new JsonSchemaBuilder().Schema(invalidUri)
                                            .Type(SchemaValueType.Object)
                                            .Build();

        Assert.Throws<BadRequestException>(() => _sut.ValidateRequestSchema(schema));
    }

    [Theory]
    [InlineData("http://example.com/schema")]
    [InlineData("https://json-schema.org/draft-07/schema")]
    [InlineData("urn:uuid:12345678-1234-1234-1234-123456789012")]
    public void ValidateRequestSchema_ValidUriScheme_DoesNotThrow(string validUri)
    {
        var schema = new JsonSchemaBuilder()
                     .Schema(validUri)
                     .Type(SchemaValueType.Object)
                     .Properties(new Dictionary<string, JsonSchema>
                     {
                         ["test"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build()
                     })
                     .Build();

        _sut.ValidateRequestSchema(schema);
    }

    [Fact]
    public void ValidateRequestSchema_UriWithMaliciousPatterns_ThrowsBadRequestException()
    {
        const string MaliciousUri = "http://example.com/<script>alert(1)</script>";
        var schema = new JsonSchemaBuilder().Id(MaliciousUri)
                                            .Type(SchemaValueType.Object)
                                            .Properties(new Dictionary<string, JsonSchema>
                                            {
                                                ["test"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build()
                                            })
                                            .Build();

        Assert.Throws<BadRequestException>(() => _sut.ValidateRequestSchema(schema));
    }

    [Fact]
    public void ValidateRequestSchema_RelativeUriWithPathTraversal_ThrowsBadRequestException()
    {
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object).Properties(new Dictionary<string, JsonSchema>
        {
            ["test"] = new JsonSchemaBuilder()
                                                           .Ref("../../malicious/path")
                                                           .Build()
        }).Build();

        Assert.Throws<BadRequestException>(() => _sut.ValidateRequestSchema(schema));
    }

    [Fact]
    public void ValidateRequestSchema_ValidSchemaWithContextPrefix_DoesNotThrow()
    {
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object)
                                            .Properties(new Dictionary<string, JsonSchema>
                                            {
                                                ["Property1_aastwinengine_00"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build(),
                                                ["Property2_aastwinengine_01"] = new JsonSchemaBuilder().Type(SchemaValueType.Integer).Build()
                                            })
                                            .Build();

        _sut.ValidateRequestSchema(schema);
    }

    [Fact]
    public void ValidateRequestSchema_NestedSchemaWithValidation_ValidatesAllLevels()
    {
        var longString = new string('a', 3000);
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object)
                                            .Properties(new Dictionary<string, JsonSchema>
                                            {
                                                ["outer"] = new JsonSchemaBuilder()
                                                            .Type(SchemaValueType.Object)
                                                            .Properties(new Dictionary<string, JsonSchema>
                                                            {
                                                                ["inner"] = new JsonSchemaBuilder()
                                                                            .Type(SchemaValueType.String)
                                                                            .Const(longString) // This should be caught
                                                            })
                                                            .Build()
                                            })
                                            .Build();

        Assert.Throws<BadRequestException>(() => _sut.ValidateRequestSchema(schema));
    }

    [Fact]
    public void ValidateRequestSchema_ArrayWithMaliciousItems_ThrowsBadRequestException()
    {
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object)
                                            .Properties(new Dictionary<string, JsonSchema>
                                            {
                                                ["items"] = new JsonSchemaBuilder()
                                                            .Type(SchemaValueType.Array)
                                                            .Items(new JsonSchemaBuilder()
                                                                   .Type(SchemaValueType.String)
                                                                   .Pattern("(a+)+") // Dangerous pattern
                                                                   .Build())
                                                            .Build()
                                            })
                                            .Build();

        Assert.Throws<BadRequestException>(() => _sut.ValidateRequestSchema(schema));
    }

    [Fact]
    public void ValidateRequestSchema_ComplexValidSchema_DoesNotThrow()
    {
        var schema = new JsonSchemaBuilder().Schema("https://json-schema.org/draft-07/schema#")
                                            .Id("https://example.com/myschema")
                                            .Type(SchemaValueType.Object)
                                            .Properties(new Dictionary<string, JsonSchema>
                                            {
                                                ["name"] = new JsonSchemaBuilder()
                                                           .Type(SchemaValueType.String)
                                                           .Pattern("^[a-zA-Z0-9_-]+$")
                                                           .MinLength(1)
                                                           .MaxLength(100)
                                                           .Build(),
                                                ["age"] = new JsonSchemaBuilder()
                                                          .Type(SchemaValueType.Integer)
                                                          .Minimum(0)
                                                          .Maximum(150)
                                                          .Build(),
                                                ["address"] = new JsonSchemaBuilder()
                                                              .Type(SchemaValueType.Object)
                                                              .Properties(new Dictionary<string, JsonSchema>
                                                              {
                                                                  ["street"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build(),
                                                                  ["city"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build()
                                                              })
                                                              .Build()
                                            })
                                            .Build();

        _sut.ValidateRequestSchema(schema);
    }

    [Fact]
    public void ValidateRequestSchema_LogsErrorForSecurityViolations()
    {
        var maliciousSchema = new JsonSchemaBuilder().Type(SchemaValueType.Object)
                                                     .Properties(new Dictionary<string, JsonSchema>
                                                     {
                                                         ["<script>"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build()
                                                     })
                                                     .Build();

        Assert.Throws<BadRequestException>(() => _sut.ValidateRequestSchema(maliciousSchema));

        _securityLogger.Received().Log(LogLevel.Error,
                                       Arg.Any<EventId>(),
                                       Arg.Is<object>(o => o.ToString()!.Contains("malicious", StringComparison.OrdinalIgnoreCase)),
                                       Arg.Any<Exception>(),
                                       Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void ValidateRequestSchema_AllowedSchemaKeywords_DoNotGetValidated()
    {
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object)
                                            .Properties(new Dictionary<string, JsonSchema>
                                            {
                                                ["normalProperty"] = new JsonSchemaBuilder()
                                                                     .Type(SchemaValueType.String)
                                                                     .Description("This is a valid description")
                                                                     .Build()
                                            })
                                            .Build();

        _sut.ValidateRequestSchema(schema);
    }

    private static JsonSchema BuildNestedSchema(int depth)
    {
        if (depth == 0)
        {
            return new JsonSchemaBuilder().Type(SchemaValueType.String).Build();
        }

        return new JsonSchemaBuilder()
                .Type(SchemaValueType.Object)
             .Properties(new Dictionary<string, JsonSchema>
             {
                 ["nested"] = BuildNestedSchema(depth - 1)
             })
           .Build();
    }

    private static JsonSchema BuildDeepArrayStructure(int depth)
    {
        if (depth == 0)
        {
            return new JsonSchemaBuilder().Type(SchemaValueType.String).Build();
        }

        return new JsonSchemaBuilder().Type(SchemaValueType.Array)
                                      .Items(new JsonSchemaBuilder()
                                             .Type(SchemaValueType.Object)
                                             .Properties(new Dictionary<string, JsonSchema>
                                             {
                                                 ["child"] = BuildDeepArrayStructure(depth - 1)
                                             })
                                             .Build())
                                      .Build();
    }
}


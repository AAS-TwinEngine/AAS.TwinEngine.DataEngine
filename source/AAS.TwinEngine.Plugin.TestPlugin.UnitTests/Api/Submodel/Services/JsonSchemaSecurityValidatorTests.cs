using System.Text.Json.Nodes;

using AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Services;
using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Exceptions;
using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.Submodel.Config;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace AAS.TwinEngine.Plugin.TestPlugin.UnitTests.Api.Submodel.Services;

public class JsonSchemaSecurityValidatorTests
{
    private readonly JsonSchemaSecurityValidator _sut;
    private readonly ILogger<JsonSchemaSecurityValidator> _logger;

    public JsonSchemaSecurityValidatorTests()
    {
        var semantics = Substitute.For<IOptions<Semantics>>();
        semantics.Value.Returns(new Semantics
        {
            IndexContextPrefix = "_aastwinengine_"
        });

        _logger = Substitute.For<ILogger<JsonSchemaSecurityValidator>>();
        _sut = new JsonSchemaSecurityValidator(semantics, _logger);
    }

    #region ValidateSchemaComplexity Tests

    [Fact]
    public void ValidateSchemaComplexity_DepthExceedsLimit_ThrowsBadRequestException()
    {
        var root = BuildNestedObject(depth: 11);

        Assert.Throws<BadRequestException>(() => _sut.ValidateSchemaComplexity(root));
    }

    [Fact]
    public void ValidateSchemaComplexity_PropertyCountExceedsLimit_ThrowsBadRequestException()
    {
        var properties = new JsonObject();
        for (var index = 0; index < 1001; index++)
        {
            properties[$"property{index}"] = new JsonObject { ["type"] = "string" };
        }

        var root = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties
        };

        Assert.Throws<BadRequestException>(() => _sut.ValidateSchemaComplexity(root));
    }

    [Fact]
    public void ValidateSchemaComplexity_ValidComplexity_DoesNotThrow()
    {
        var properties = new JsonObject();
        for (var index = 0; index < 1000; index++)
        {
            properties[$"property{index}"] = new JsonObject { ["type"] = "string" };
        }

        var root = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties
        };

        _sut.ValidateSchemaComplexity(root);
    }

    [Fact]
    public void ValidateSchemaComplexity_EmptyObject_DoesNotThrow()
    {
        var root = new JsonObject
        {
            ["type"] = "object"
        };

        _sut.ValidateSchemaComplexity(root);
    }

    [Fact]
    public void ValidateSchemaComplexity_SingleValue_DoesNotThrow()
    {
        var root = JsonValue.Create("string");

        _sut.ValidateSchemaComplexity(root!);
    }

    [Fact]
    public void ValidateSchemaComplexity_EmptyArray_DoesNotThrow()
    {
        var root = new JsonArray();

        _sut.ValidateSchemaComplexity(root);
    }

    [Fact]
    public void ValidateSchemaComplexity_ArrayWithObjects_CountsDepthCorrectly()
    {
        var root = new JsonObject
        {
            ["type"] = "array",
            ["items"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["field"] = new JsonObject { ["type"] = "string" }
                    }
                }
            }
        };

        _sut.ValidateSchemaComplexity(root);
    }

    [Fact]
    public void ValidateSchemaComplexity_MixedArrayAndObjectNesting_ChecksDepthCorrectly()
    {
        var root = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["items"] = new JsonObject
                {
                    ["type"] = "array",
                    ["items"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["type"] = "object",
                            ["properties"] = new JsonObject
                            {
                                ["nested"] = BuildNestedObject(depth: 7)
                            }
                        }
                    }
                }
            }
        };

        Assert.Throws<BadRequestException>(() => _sut.ValidateSchemaComplexity(root));
    }

    [Fact]
    public void ValidateSchemaComplexity_NullValuesInArray_DoesNotThrow()
    {
        var root = new JsonObject
        {
            ["type"] = "array",
            ["items"] = new JsonArray
            {
                null,
                new JsonObject { ["type"] = "string" },
                null
            }
        };

        _sut.ValidateSchemaComplexity(root);
    }

    [Fact]
    public void ValidateSchemaComplexity_NullValueInObject_DoesNotThrow()
    {
        var root = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["field1"] = null,
                ["field2"] = new JsonObject { ["type"] = "string" }
            }
        };

        _sut.ValidateSchemaComplexity(root);
    }

    [Fact]
    public void ValidateSchemaComplexity_MultiplePropertiesAtDifferentLevels_CountsCorrectly()
    {
        var level2Props = new JsonObject();
        for (var i = 0; i < 500; i++)
        {
            level2Props[$"prop{i}"] = new JsonObject { ["type"] = "string" };
        }

        var level1Props = new JsonObject();
        for (var i = 0; i < 500; i++)
        {
            level1Props[$"prop{i}"] = new JsonObject { ["type"] = "integer" };
        }

        level1Props["nested"] = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = level2Props
        };

        var root = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = level1Props
        };

        Assert.Throws<BadRequestException>(() => _sut.ValidateSchemaComplexity(root));
    }

    #endregion

    #region ValidateSchemaContent Tests

    [Fact]
    public void ValidateSchemaContent_MaliciousPropertyName_ThrowsBadRequestException()
    {
        var root = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["<script>"] = new JsonObject { ["type"] = "string" }
            }
        };

        Assert.Throws<BadRequestException>(() => _sut.ValidateSchemaContent(root));
    }

    [Fact]
    public void ValidateSchemaContent_InvalidUriScheme_ThrowsBadRequestException()
    {
        var root = new JsonObject
        {
            ["$schema"] = "ftp://example.com/schema",
            ["type"] = "object"
        };

        Assert.Throws<BadRequestException>(() => _sut.ValidateSchemaContent(root));
    }

    [Fact]
    public void ValidateSchemaContent_DangerousRegexPattern_ThrowsBadRequestException()
    {
        var root = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["field"] = new JsonObject
                {
                    ["type"] = "string",
                    ["pattern"] = "(a+)+"
                }
            }
        };

        Assert.Throws<BadRequestException>(() => _sut.ValidateSchemaContent(root));
    }

    [Fact]
    public void ValidateSchemaContent_StringWithNullByte_ThrowsBadRequestException()
    {
        var root = new JsonObject
        {
            ["type"] = "object",
            ["const"] = "abc%00xyz"
        };

        Assert.Throws<BadRequestException>(() => _sut.ValidateSchemaContent(root));
    }

    [Fact]
    public void ValidateSchemaContent_ContextSuffixedProperty_DoesNotThrow()
    {
        var root = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["Property_aastwinengine_00"] = new JsonObject
                {
                    ["type"] = "string"
                }
            }
        };

        _sut.ValidateSchemaContent(root);
    }

    [Theory]
    [InlineData("<script>_aastwinengine_00")]
    [InlineData("'; DROP TABLE_aastwinengine_01")]
    [InlineData("../../etc/passwd_aastwinengine_")]
    [InlineData("javascript:alert(1)_aastwinengine_99")]
    public void ValidateSchemaContent_MaliciousPropertyWithContextSuffix_ThrowsBadRequestException(string maliciousPropertyName)
    {
        var root = CreateSchemaWithProperty(maliciousPropertyName,
            new JsonObject { ["type"] = "string" });

        Assert.Throws<BadRequestException>(() => _sut.ValidateSchemaContent(root));
    }

    [Theory]
    [InlineData("ValidProperty_aastwinengine_00")]
    [InlineData("MyProperty_aastwinengine_01")]
    [InlineData("Property123_aastwinengine_99")]
    [InlineData("test_property_aastwinengine_")]
    [InlineData("simple_aastwinengine_123")]
    public void ValidateSchemaContent_ValidPropertyWithContextSuffix_DoesNotThrow(string validPropertyName)
    {
        var root = CreateSchemaWithProperty(validPropertyName,
            new JsonObject { ["type"] = "string" });

        _sut.ValidateSchemaContent(root);
    }

    [Theory]
    [InlineData("ValidProperty_aastwinengine_abc")] // Non-digit after prefix
    [InlineData("Property_aastwinengine_#")] // Invalid character
    [InlineData("test_aastwinengine_-1")] // Negative number (hyphen)
    public void ValidateSchemaContent_InvalidContextSuffixFormat_ThrowsBadRequestException(string invalidPropertyName)
    {
        var root = CreateSchemaWithProperty(invalidPropertyName,
               new JsonObject { ["type"] = "string" });

        Assert.Throws<BadRequestException>(() => _sut.ValidateSchemaContent(root));
    }

    [Fact]
    public void ValidateSchemaContent_MaliciousPropertyWithMultipleContextPrefixes_ThrowsBadRequestException()
    {
        var root = CreateSchemaWithProperty("<script>_aastwinengine_00_aastwinengine_01",
            new JsonObject { ["type"] = "string" });

        Assert.Throws<BadRequestException>(() => _sut.ValidateSchemaContent(root));
    }

    [Fact]
    public void ValidateSchemaContent_ContextSuffixOnly_ThrowsBadRequestException()
    {
        var root = CreateSchemaWithProperty("_aastwinengine_00",
             new JsonObject { ["type"] = "string" });

        Assert.Throws<BadRequestException>(() => _sut.ValidateSchemaContent(root));
    }

    [Fact]
    public void ValidateSchemaContent_ValidationFailure_LogsError()
    {
        var root = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["<script>"] = new JsonObject { ["type"] = "string" }
            }
        };

        Assert.Throws<BadRequestException>(() => _sut.ValidateSchemaContent(root));

        _logger.Received().Log(LogLevel.Error,
                               Arg.Any<EventId>(),
                               Arg.Any<object>(),
                               Arg.Any<Exception>(),
                               Arg.Any<Func<object, Exception?, string>>());
    }

    [Theory]
    [InlineData("$customKeyword")]
    [InlineData("$myExtension")]
    [InlineData("$x")]
    public void ValidateSchemaContent_PropertyStartingWithDollarSign_DoesNotValidateAsIdentifier(string propertyName)
    {
        var root = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                [propertyName] = new JsonObject { ["type"] = "string" }
            }
        };

        _sut.ValidateSchemaContent(root);
    }

    [Theory]
    [InlineData("$")]
    [InlineData("$$")]
    [InlineData("$123")]
    public void ValidateSchemaContent_EdgeCasePropertiesStartingWithDollar_DoesNotThrow(string propertyName)
    {
        var root = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                [propertyName] = new JsonObject { ["type"] = "string" }
            }
        };

        _sut.ValidateSchemaContent(root);
    }

    [Fact]
    public void ValidateSchemaContent_AllowedKeyword_DoesNotValidateAsIdentifier()
    {
        var root = new JsonObject
        {
            ["type"] = "object",
            ["title"] = "My Schema",
            ["description"] = "A test schema"
        };

        _sut.ValidateSchemaContent(root);
    }

    [Fact]
    public void ValidateSchemaContent_PropertyWithContextPrefix_DoesNotValidateAfterCleanup()
    {
        var root = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["ValidProperty_aastwinengine_00"] = new JsonObject { ["type"] = "string" }
            }
        };

        _sut.ValidateSchemaContent(root);
    }

    [Fact]
    public void ValidateSchemaContent_PropertyNotStartingWithDollar_ValidatesAsIdentifier()
    {
        var root = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["normalProperty"] = new JsonObject { ["type"] = "string" }
            }
        };

        _sut.ValidateSchemaContent(root);
    }

    [Theory]
    [InlineData("a$property")]
    [InlineData("my$custom")]
    [InlineData("test$123")]
    public void ValidateSchemaContent_PropertyWithDollarInMiddle_ValidatesAsIdentifier(string propertyName)
    {
        var root = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                [propertyName] = new JsonObject { ["type"] = "string" }
            }
        };

        _sut.ValidateSchemaContent(root);
    }

    [Fact]
    public void ValidateSchemaContent_MaliciousPropertyNotStartingWithDollar_ThrowsBadRequestException()
    {
        var root = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["../../etc/passwd"] = new JsonObject { ["type"] = "string" }
            }
        };

        Assert.Throws<BadRequestException>(() => _sut.ValidateSchemaContent(root));
    }

    [Fact]
    public void ValidateSchemaContent_EmptyPropertyName_ThrowsBadRequestException()
    {
        var root = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                [""] = new JsonObject { ["type"] = "string" }
            }
        };

        Assert.Throws<BadRequestException>(() => _sut.ValidateSchemaContent(root));
    }

    [Fact]
    public void ValidateSchemaContent_EmptyObject_DoesNotThrow()
    {
        var root = new JsonObject();

        _sut.ValidateSchemaContent(root);
    }

    [Fact]
    public void ValidateSchemaContent_EmptyArray_DoesNotThrow()
    {
        var root = new JsonArray();

        _sut.ValidateSchemaContent(root);
    }

    [Fact]
    public void ValidateSchemaContent_SingleJsonValue_DoesNotThrow()
    {
        var root = JsonValue.Create("test");

        _sut.ValidateSchemaContent(root!);
    }

    [Fact]
    public void ValidateSchemaContent_NumberValue_DoesNotThrow()
    {
        var root = JsonValue.Create(123);

        _sut.ValidateSchemaContent(root!);
    }

    [Fact]
    public void ValidateSchemaContent_BooleanValue_DoesNotThrow()
    {
        var root = JsonValue.Create(true);

        _sut.ValidateSchemaContent(root!);
    }

    [Fact]
    public void ValidateSchemaContent_ArrayWithNullValues_DoesNotThrow()
    {
        var root = new JsonArray
        {
            null,
            new JsonObject { ["type"] = "string" },
            null
        };

        _sut.ValidateSchemaContent(root);
    }

    [Fact]
    public void ValidateSchemaContent_NestedArrays_ValidatesAllLevels()
    {
        var root = new JsonObject
        {
            ["type"] = "array",
            ["items"] = new JsonArray
            {
                new JsonArray
                {
                    new JsonObject
                    {
                        ["properties"] = new JsonObject
                        {
                            ["<script>"] = new JsonObject { ["type"] = "string" }
                        }
                    }
                }
            }
        };

        Assert.Throws<BadRequestException>(() => _sut.ValidateSchemaContent(root));
    }

    [Theory]
    [InlineData("http://example.com/schema")]
    [InlineData("https://json-schema.org/draft-07/schema")]
    [InlineData("urn:uuid:12345678-1234-1234-1234-123456789012")]
    public void ValidateSchemaContent_ValidUriSchemes_DoesNotThrow(string uri)
    {
        var root = new JsonObject
        {
            ["$schema"] = uri,
            ["type"] = "object"
        };

        _sut.ValidateSchemaContent(root);
    }

    [Theory]
    [InlineData("file:///etc/passwd")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    public void ValidateSchemaContent_InvalidUriSchemes_ThrowsBadRequestException(string uri)
    {
        var root = new JsonObject
        {
            ["$schema"] = uri,
            ["type"] = "object"
        };

        Assert.Throws<BadRequestException>(() => _sut.ValidateSchemaContent(root));
    }

    [Fact]
    public void ValidateSchemaContent_VeryLongPropertyName_ThrowsBadRequestException()
    {
        var longName = new string('a', 300);
        var root = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                [longName] = new JsonObject { ["type"] = "string" }
            }
        };

        Assert.Throws<BadRequestException>(() => _sut.ValidateSchemaContent(root));
    }

    [Fact]
    public void ValidateSchemaContent_PropertyNameAtMaxLength_DoesNotThrow()
    {
        var maxName = new string('a', 256);
        var root = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                [maxName] = new JsonObject { ["type"] = "string" }
            }
        };

        _sut.ValidateSchemaContent(root);
    }

    [Fact]
    public void ValidateSchemaContent_VeryLongStringValue_ThrowsBadRequestException()
    {
        var longString = new string('a', 3000);
        var root = new JsonObject
        {
            ["type"] = "object",
            ["const"] = longString
        };

        Assert.Throws<BadRequestException>(() => _sut.ValidateSchemaContent(root));
    }

    [Fact]
    public void ValidateSchemaContent_StringValueAtMaxLength_DoesNotThrow()
    {
        var maxString = new string('a', 2048);
        var root = new JsonObject
        {
            ["type"] = "object",
            ["const"] = maxString
        };

        _sut.ValidateSchemaContent(root);
    }

    [Fact]
    public void ValidateSchemaContent_VeryLongUri_ThrowsBadRequestException()
    {
        var longUri = "http://example.com/" + new string('a', 2100);
        var root = new JsonObject
        {
            ["$id"] = longUri,
            ["type"] = "object"
        };

        Assert.Throws<BadRequestException>(() => _sut.ValidateSchemaContent(root));
    }

    [Fact]
    public void ValidateSchemaContent_UriAtMaxLength_DoesNotThrow()
    {
        var maxUri = "http://example.com/" + new string('a', 2020);
        var root = new JsonObject
        {
            ["$id"] = maxUri,
            ["type"] = "object"
        };

        _sut.ValidateSchemaContent(root);
    }

    [Fact]
    public void ValidateSchemaContent_VeryLongRegexPattern_ThrowsBadRequestException()
    {
        var longPattern = new string('a', 600);
        var root = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["field"] = new JsonObject
                {
                    ["type"] = "string",
                    ["pattern"] = longPattern
                }
            }
        };

        Assert.Throws<BadRequestException>(() => _sut.ValidateSchemaContent(root));
    }

    [Fact]
    public void ValidateSchemaContent_PatternAtMaxLength_DoesNotThrow()
    {
        var maxPattern = new string('a', 512);
        var root = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["field"] = new JsonObject
                {
                    ["type"] = "string",
                    ["pattern"] = maxPattern
                }
            }
        };

        _sut.ValidateSchemaContent(root);
    }

    [Theory]
    [InlineData("(a*)*")]
    [InlineData("(a+)*")]
    [InlineData("(a*)+")]
    [InlineData("(a{1,})+")]
    [InlineData("(a{1,})*")]
    public void ValidateSchemaContent_DangerousRegexPatterns_ThrowsBadRequestException(string pattern)
    {
        var root = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["field"] = new JsonObject
                {
                    ["type"] = "string",
                    ["pattern"] = pattern
                }
            }
        };

        Assert.Throws<BadRequestException>(() => _sut.ValidateSchemaContent(root));
    }

    [Theory]
    [InlineData("^[a-zA-Z0-9]+$")]
    [InlineData("^\\d{3}-\\d{2}-\\d{4}$")]
    [InlineData("[a-z]{1,10}")]
    public void ValidateSchemaContent_SafeRegexPatterns_DoesNotThrow(string pattern)
    {
        var root = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["field"] = new JsonObject
                {
                    ["type"] = "string",
                    ["pattern"] = pattern
                }
            }
        };

        _sut.ValidateSchemaContent(root);
    }

    [Fact]
    public void ValidateSchemaContent_InvalidRegexSyntax_ThrowsBadRequestException()
    {
        var root = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["field"] = new JsonObject
                {
                    ["type"] = "string",
                    ["pattern"] = "[unclosed"
                }
            }
        };

        Assert.Throws<BadRequestException>(() => _sut.ValidateSchemaContent(root));
    }

    [Fact]
    public void ValidateSchemaContent_EmptyUri_DoesNotThrow()
    {
        var root = new JsonObject
        {
            ["$schema"] = "",
            ["type"] = "object"
        };

        _sut.ValidateSchemaContent(root);
    }

    [Fact]
    public void ValidateSchemaContent_WhitespaceUri_DoesNotThrow()
    {
        var root = new JsonObject
        {
            ["$schema"] = "   ",
            ["type"] = "object"
        };

        _sut.ValidateSchemaContent(root);
    }

    [Fact]
    public void ValidateSchemaContent_PathTraversalInRef_ThrowsBadRequestException()
    {
        var root = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["test"] = new JsonObject
                {
                    ["$ref"] = "../../malicious/path"
                }
            }
        };

        Assert.Throws<BadRequestException>(() => _sut.ValidateSchemaContent(root));
    }

    [Fact]
    public void ValidateSchemaContent_ValidFragmentRef_DoesNotThrow()
    {
        var root = new JsonObject
        {
            ["type"] = "object",
            ["$defs"] = new JsonObject
            {
                ["myDef"] = new JsonObject { ["type"] = "string" }
            },
            ["properties"] = new JsonObject
            {
                ["test"] = new JsonObject
                {
                    ["$ref"] = "#/definitions/myDef"
                }
            }
        };

        _sut.ValidateSchemaContent(root);
    }

    [Fact]
    public void ValidateSchemaContent_UriWithMaliciousPatterns_ThrowsBadRequestException()
    {
        var root = new JsonObject
        {
            ["$id"] = "http://example.com/<script>alert(1)</script>",
            ["type"] = "object"
        };

        Assert.Throws<BadRequestException>(() => _sut.ValidateSchemaContent(root));
    }

    [Theory]
    [InlineData("title")]
    [InlineData("description")]
    [InlineData("default")]
    [InlineData("examples")]
    [InlineData("enum")]
    [InlineData("type")]
    [InlineData("properties")]
    [InlineData("required")]
    [InlineData("items")]
    public void ValidateSchemaContent_AllowedSchemaKeywords_DoesNotValidate(string keyword)
    {
        var root = new JsonObject
        {
            [keyword] = "any-value-here"
        };

        _sut.ValidateSchemaContent(root);
    }

    [Theory]
    [InlineData("property🔒")]
    [InlineData("属性名称")]
    [InlineData("свойство")]
    public void ValidateSchemaContent_UnicodePropertyNames_HandledCorrectly(string propertyName)
    {
        var root = CreateSchemaWithProperty(propertyName,
                                            new JsonObject { ["type"] = "string" });

        _sut.ValidateSchemaContent(root);
    }

    [Fact]
    public void ValidateSchemaContent_ConcurrentValidation_ThreadSafe()
    {
        var root = new JsonObject { ["type"] = "object" };
        var tasks = Enumerable.Range(0, 10)
                              .Select(_ => Task.Run(() => _sut.ValidateSchemaContent(root)))
                              .ToArray();

        Task.WaitAll(tasks);
    }

    [Fact]
    public void ValidateSchemaContent_ComplexRealWorldSchema_DoesNotThrow()
    {
        var root = new JsonObject
        {
            ["$schema"] = "https://json-schema.org/draft-07/schema#",
            ["$id"] = "https://example.com/product.schema.json",
            ["type"] = "object",
            ["title"] = "Product",
            ["description"] = "A product from Acme's catalog",
            ["properties"] = new JsonObject
            {
                ["productId"] = new JsonObject
                {
                    ["type"] = "integer",
                    ["description"] = "The unique identifier for a product"
                },
                ["productName"] = new JsonObject
                {
                    ["type"] = "string",
                    ["description"] = "Name of the product",
                    ["pattern"] = "^[a-zA-Z0-9 ]+$"
                },
                ["tags"] = new JsonObject
                {
                    ["type"] = "array",
                    ["items"] = new JsonObject
                    {
                        ["type"] = "string"
                    }
                }
            },
            ["required"] = new JsonArray { "productId", "productName" }
        };

        _sut.ValidateSchemaContent(root);
    }

    #endregion

    #region Helper Methods

    private static JsonNode BuildNestedObject(int depth)
    {
        if (depth == 0)
        {
            return new JsonObject { ["type"] = "string" };
        }

        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["child"] = BuildNestedObject(depth - 1)
            }
        };
    }

    private static JsonObject CreateSchemaWithProperty(string propertyName, JsonNode value)
    {
        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                [propertyName] = value
            }
        };
    }

    #endregion
}

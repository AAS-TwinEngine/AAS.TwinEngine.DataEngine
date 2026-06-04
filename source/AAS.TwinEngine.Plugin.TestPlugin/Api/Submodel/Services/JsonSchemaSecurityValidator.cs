using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Exceptions;
using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Extensions;
using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.Submodel.Config;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Services;

public class JsonSchemaSecurityValidator(IOptions<Semantics> semantics, ILogger<JsonSchemaSecurityValidator> logger) : IJsonSchemaSecurityValidator
{
    private readonly string _contextPrefix = semantics.Value.IndexContextPrefix;

    private const int MaxSchemaDepth = 10;
    private const int MaxProperties = 1000;
    private const int MaxPropertyNameLength = 256;
    private const int MaxStringValueLength = 2048;
    private const int MaxPatternLength = 512;
    private const int MaxUriLength = 2048;

    private static readonly TimeSpan RegexValidationTimeout = TimeSpan.FromMilliseconds(100);

    private static readonly HashSet<string> AllowedSchemaKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "$schema", "$id", "$ref", "$comment",
        "type", "properties", "required", "items", "definitions", "$defs",
        "title", "description", "default", "examples",
        "enum", "const",
        "minimum", "maximum", "exclusiveMinimum", "exclusiveMaximum",
        "minLength", "maxLength", "pattern", "format",
        "minItems", "maxItems", "uniqueItems",
        "minProperties", "maxProperties",
        "additionalProperties", "patternProperties",
        "allOf", "anyOf", "oneOf", "not",
        "multipleOf", "minContains", "maxContains"
    };

    private static readonly HashSet<string> AllowedUriSchemes = new(StringComparer.OrdinalIgnoreCase)
    {
        "http", "https", "urn"
    };

    public void ValidateSchemaComplexity(JsonNode rootNode)
    {
        var stack = new Stack<(JsonNode node, int depth)>();
        stack.Push((rootNode, 0));

        var totalPropertiesCount = 0;

        while (stack.Count > 0)
        {
            var (current, depth) = stack.Pop();

            ValidateDepth(depth);

            totalPropertiesCount = ProcessNode(current, depth, totalPropertiesCount, stack);
        }
    }

    private static void ValidateDepth(int depth)
    {
        if (depth > MaxSchemaDepth)
        {
            throw new BadRequestException($"Schema nesting too deep. Maximum allowed depth is {MaxSchemaDepth}.");
        }
    }

    private static int ProcessNode(JsonNode node, int depth, int totalPropertiesCount, Stack<(JsonNode node, int depth)> stack)
    {
        return node switch
        {
            JsonObject obj => ProcessJsonObject(obj, depth, totalPropertiesCount, stack),
            JsonArray arr => ProcessJsonArray(arr, depth, stack, totalPropertiesCount),
            _ => totalPropertiesCount
        };
    }

    private static int ProcessJsonObject(JsonObject obj, int depth, int totalPropertiesCount, Stack<(JsonNode node, int depth)> stack)
    {
        var updatedCount = ValidateAndCountProperties(obj, totalPropertiesCount);
        EnqueueChildNodesForValidation(obj, depth, stack);
        return updatedCount;
    }

    private static int ValidateAndCountProperties(JsonObject obj, int currentCount)
    {
        if (!obj.TryGetPropertyValue("properties", out var propsNode) || propsNode is not JsonObject propsObj)
        {
            return currentCount;
        }

        var newCount = currentCount + propsObj.Count;
        if (newCount > MaxProperties)
        {
            throw new BadRequestException($"Schema contains too many properties. Maximum allowed is {MaxProperties}.");
        }

        return newCount;
    }

    private static void EnqueueChildNodesForValidation(JsonObject obj, int depth, Stack<(JsonNode node, int depth)> stack)
    {
        foreach (var childNode in obj.Select(kv => kv.Value).Where(value => value != null))
        {
            stack.Push((childNode!, depth + 1));
        }
    }

    private static int ProcessJsonArray(JsonArray arr, int depth, Stack<(JsonNode node, int depth)> stack, int totalPropertiesCount)
    {
        foreach (var childNode in arr.Where(item => item != null))
        {
            stack.Push((childNode!, depth + 1));
        }

        return totalPropertiesCount;
    }

    public void ValidateSchemaContent(JsonNode rootNode)
    {
        var stack = new Stack<JsonNode>();
        stack.Push(rootNode);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            ProcessNodeForContentValidation(current, stack);
        }
    }

    private void ProcessNodeForContentValidation(JsonNode node, Stack<JsonNode> stack)
    {
        switch (node)
        {
            case JsonObject obj:
                ProcessJsonObjectContent(obj, stack);
                break;

            case JsonArray arr:
                ProcessJsonArrayContent(arr, stack);
                break;

            case JsonValue value:
                ValidateJsonValue(value);
                break;
        }
    }

    private void ProcessJsonObjectContent(JsonObject obj, Stack<JsonNode> stack)
    {
        ValidateJsonObject(obj);
        foreach (var childNode in obj.Select(p => p.Value).Where(value => value != null))
        {
            stack.Push(childNode!);
        }
    }

    private static void ProcessJsonArrayContent(JsonArray arr, Stack<JsonNode> stack)
    {
        foreach (var item in arr.Where(item => item != null))
        {
            stack.Push(item!);
        }
    }

    private void ValidateJsonObject(JsonObject obj)
    {
        foreach (var property in obj)
        {
            ValidateProperty(property);
        }
    }

    private void ValidateProperty(KeyValuePair<string, JsonNode?> property)
    {
        var propertyName = property.Key;

        ValidatePropertyNameLength(propertyName);
        ValidatePropertyNameSafety(propertyName);
        ValidateSpecialPropertyValues(property);
    }

    private static void ValidatePropertyNameLength(string propertyName)
    {
        if (propertyName.Length > MaxPropertyNameLength)
        {
            throw new BadRequestException($"Property name exceeds maximum length of {MaxPropertyNameLength} characters: {propertyName[..Math.Min(50, propertyName.Length)]}...");
        }
    }

    private void ValidatePropertyNameSafety(string propertyName)
    {
        if (AllowedSchemaKeywords.Contains(propertyName) ||
            propertyName.StartsWith('$'))
        {
            return;
        }

        var nameWithoutContextSuffix = RemoveContextSuffix(propertyName);

        if (!nameWithoutContextSuffix.IsValidIdentifier())
        {
            ThrowBadRequestException($"Property name contains potentially malicious patterns: {propertyName}");
        }

        if (nameWithoutContextSuffix != propertyName)
        {
            ValidateContextSuffix(propertyName, nameWithoutContextSuffix.Length);
        }
    }

    private void ValidateContextSuffix(string propertyName, int nameWithoutContextSuffixLength)
    {
        var suffix = propertyName[nameWithoutContextSuffixLength..];

        if (!suffix.StartsWith(_contextPrefix, StringComparison.Ordinal))
        {
            ThrowBadRequestException($"Invalid context suffix format in property name: {propertyName}");
        }

        var suffixAfterPrefix = suffix[_contextPrefix.Length..];

        if (!string.IsNullOrEmpty(suffixAfterPrefix) && !suffixAfterPrefix.All(char.IsDigit))
        {
            ThrowBadRequestException($"Context suffix must contain only digits after prefix: {propertyName}");
        }
    }

    private void ValidateSpecialPropertyValues(KeyValuePair<string, JsonNode?> property)
    {
        var propertyName = property.Key;

        switch (propertyName)
        {
            case "$ref":
            case "$id":
            case "$schema":
                if (property.Value is JsonValue refValue && refValue.TryGetValue<string>(out var uriString))
                {
                    ValidateUri(uriString, propertyName);
                }

                break;

            case "pattern":
                if (property.Value is JsonValue patternValue && patternValue.TryGetValue<string>(out var pattern))
                {
                    ValidateRegexPattern(pattern);
                }

                break;
        }
    }

    private void ValidateJsonValue(JsonValue value)
    {
        if (!value.TryGetValue<string>(out var stringValue))
        {
            return;
        }

        if (stringValue.Length > MaxStringValueLength)
        {
            ThrowBadRequestException($"String value exceeds maximum length of {MaxStringValueLength} characters.");
        }

        if (stringValue.Contains('\0', StringComparison.Ordinal) ||
            stringValue.Contains("%00", StringComparison.OrdinalIgnoreCase))
        {
            ThrowBadRequestException("String value contains null byte characters.");
        }
    }

    private void ValidateUri(string uriString, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(uriString))
        {
            return;
        }

        if (uriString.Length > MaxUriLength)
        {
            ThrowBadRequestException($"URI in '{propertyName}' exceeds maximum length of {MaxUriLength} characters.");
        }

        if (!uriString.IsValidIdentifier())
        {
            ThrowBadRequestException($"URI in '{propertyName}' contains potentially malicious patterns: {uriString[..Math.Min(50, uriString.Length)]}");
        }

        if (Uri.TryCreate(uriString, UriKind.Absolute, out var uri))
        {
            if (!AllowedUriSchemes.Contains(uri.Scheme))
            {
                ThrowBadRequestException($"URI scheme '{uri.Scheme}' is not allowed in '{propertyName}'. Allowed schemes: {string.Join(", ", AllowedUriSchemes)}");
            }
        }
        else if (uriString.Contains("..", StringComparison.Ordinal) &&
            !uriString.StartsWith("#/", StringComparison.Ordinal))
        {
            ThrowBadRequestException($"URI in '{propertyName}' contains potential path traversal pattern.");
        }
    }

    private void ValidateRegexPattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return;
        }

        if (pattern.Length > MaxPatternLength)
        {
            ThrowBadRequestException($"Regex pattern exceeds maximum length of {MaxPatternLength} characters.");
        }

        if (ContainsDangerousRegexPattern(pattern))
        {
            ThrowBadRequestException("Regex pattern contains potentially dangerous constructs that could cause ReDoS attacks.");
        }

        try
        {
            _ = new Regex(pattern, RegexOptions.None, RegexValidationTimeout);
        }
        catch (ArgumentException ex)
        {
            ThrowBadRequestException($"Invalid regex pattern: {ex.Message}");
        }
        catch (RegexMatchTimeoutException)
        {
            ThrowBadRequestException("Regex pattern is too complex and could cause performance issues.");
        }
    }

    private static bool ContainsDangerousRegexPattern(string pattern)
    {
        var dangerousPatterns = new[]
        {
            @"\([^)]*\+[^)]*\)\+",
            @"\([^)]*\*[^)]*\)\*",
            @"\([^)]*\+[^)]*\)\*",
            @"\([^)]*\*[^)]*\)\+",
            @"\([^)]*\{[0-9]+,\}[^)]*\)\+",
            @"\([^)]*\{[0-9]+,\}[^)]*\)\*"
        };

        foreach (var dangerousPattern in dangerousPatterns)
        {
            try
            {
                if (Regex.IsMatch(pattern, dangerousPattern, RegexOptions.None, TimeSpan.FromMilliseconds(50)))
                {
                    return true;
                }
            }
            catch (RegexMatchTimeoutException)
            {
                return true;
            }
        }

        return false;
    }

    private string RemoveContextSuffix(string propertyName)
    {
        var suffixIndex = propertyName.IndexOf(_contextPrefix, StringComparison.Ordinal);
        return suffixIndex >= 0 ? propertyName[..suffixIndex] : propertyName;
    }

    private void ThrowBadRequestException(string message)
    {
        logger.LogError("Validation error: {Message}", message);
        throw new BadRequestException();
    }
}


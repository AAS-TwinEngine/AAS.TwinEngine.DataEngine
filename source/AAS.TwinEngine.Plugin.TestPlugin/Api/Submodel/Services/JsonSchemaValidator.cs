using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Constants;
using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Exceptions;
using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.Submodel.Config;

using Json.Schema;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Services;

public class JsonSchemaValidator(IOptions<Semantics> semantics, ILogger<JsonSchemaValidator> logger) : IJsonSchemaValidator
{
    private readonly string _contextPrefix = semantics.Value.IndexContextPrefix;
    private const string DefinitionsPrefix = "#/definitions/";
    private const string DefsPrefix = "#/$defs/";
    private const string Draft7MetaSchema = "http://json-schema.org/draft-07/schema#";
    private const string Draft202012MetaSchema = "https://json-schema.org/draft/2020-12/schema";

    private static readonly JsonSerializerOptions Serialization = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public void ValidateRequestSchema(JsonSchema schema)
    {
        if (!TrySerializeSchema(schema!, out var schemaText, out var serializationError))
        {
            LogAndThrowRequestException($"Schema serialization failed: {serializationError}");
        }

        if (!TryParseSchemaNode(schemaText, out var schemaNode, out var parseError))
        {
            LogAndThrowRequestException($"Schema JSON is invalid: {parseError}");
        }

        if (schemaNode == null)
        {
            LogAndThrowRequestException("Serialized schema resulted in null JsonNode.");
        }

        try
        {
            using var schemaDocument = JsonDocument.Parse(schemaNode.ToJsonString());
            var metaSchema = ResolveMetaSchema(schemaDocument.RootElement);
            var result = metaSchema.Evaluate(schemaDocument.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
            if (!result.IsValid)
            {
                LogAndThrowRequestException("Schema is not valid against declared JSON Schema draft.");
            }
        }
        catch (Exception ex)
        {
            LogAndThrowRequestException("JSON Schema evaluation failed.", ex);
        }
    }

    public void ValidateResponseContent(string responseJson, JsonSchema requestSchema)
    {
        if (string.IsNullOrWhiteSpace(responseJson))
        {
            LogAndThrowResponseException("Response JSON is empty.");
        }

        if (!TryParseJson(responseJson, out var responseDoc, out var parseError))
        {
            LogAndThrowResponseException($"Failed to parse response JSON: {parseError}");
        }

        JsonObject normalizedSchema;

        try
        {
            normalizedSchema = NormalizeSchema(requestSchema);
        }
        catch (Exception ex)
        {
            LogAndThrowResponseException($"Failed to normalize request schema: Schema normalization failed: {ex.Message}");
            return;
        }

        try
        {
            ValidateResponseAgainstSchema(responseDoc!.RootElement, normalizedSchema);
        }
        catch (Exception ex)
        {
            LogAndThrowResponseException("Exception occurred during response validation.", ex);
        }
    }

    private void ValidateResponseAgainstSchema(JsonElement responseElement, JsonObject schemaNode)
    {
        var schema = JsonSchema.FromText(schemaNode.ToJsonString());
        var result = schema.Evaluate(responseElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
        if (!result.IsValid)
        {
            LogAndThrowResponseException("Response did not validate against schema.");
        }
    }

    [DoesNotReturn]
    private void LogAndThrowRequestException(string logMessage, Exception? ex = null)
    {
        if (ex != null)
        {
            logger.LogError(ex, logMessage);
        }
        else
        {
            logger.LogError(logMessage);
        }

        throw new BadRequestException(logMessage);
    }

    [DoesNotReturn]
    private void LogAndThrowResponseException(string logMessage, Exception? ex = null)
    {
        if (ex != null)
        {
            logger.LogError(ex, logMessage);
        }
        else
        {
            logger.LogError(logMessage);
        }

        throw new NotFoundException(logMessage);
    }

    private static bool TrySerializeSchema(JsonSchema schema, out string schemaText, out string? error)
    {
        error = null;
        schemaText = string.Empty;

        try
        {
            schemaText = JsonSerializer.Serialize(schema, Serialization);
            return true;
        }
        catch (Exception ex)
        {
            error = $"Serialization failed: {ex.Message}";
            return false;
        }
    }

    private static bool TryParseSchemaNode(string schemaText, out JsonNode? node, out string? error)
    {
        error = null;
        node = null;

        try
        {
            node = JsonNode.Parse(schemaText);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static bool TryParseJson(string json, out JsonDocument? document, out string? error)
    {
        error = null;
        document = null;

        try
        {
            document = JsonDocument.Parse(json);
            return true;
        }
        catch (Exception ex)
        {
            error = $"JSON parsing failed: {ex.Message}";
            return false;
        }
    }

    private JsonObject NormalizeSchema(JsonSchema schema)
    {
        var json = JsonSerializer.Serialize(schema, Serialization);

        var normalized = JsonNode.Parse(json)?.AsObject()
            ?? throw new ArgumentException("Failed to parse schema JSON.");

        EscapeJsonReferencePointers(normalized);
        RemoveSchemaIds(normalized);

        var declaredDraft = ResolveDraftFromSchemaNode(normalized);
        normalized["$schema"] = declaredDraft;

        return normalized;
    }

    private static string ResolveDraftFromSchemaNode(JsonNode? schemaNode)
    {
        var declared = schemaNode?["$schema"]?.GetValue<string>();
        return string.Equals(declared, Draft7MetaSchema, StringComparison.OrdinalIgnoreCase)
            ? Draft7MetaSchema
            : Draft202012MetaSchema;
    }

    private static JsonSchema ResolveMetaSchema(JsonElement schemaRoot)
    {
        if (schemaRoot.TryGetProperty("$schema", out var schemaElement) && schemaElement.ValueKind == JsonValueKind.String)
        {
            var declared = schemaElement.GetString();
            if (string.Equals(declared, MetaSchemas.Draft7Id.OriginalString, StringComparison.OrdinalIgnoreCase))
            {
                return MetaSchemas.Draft7;
            }
        }

        return MetaSchemas.Draft202012;
    }

    private static void RemoveSchemaIds(JsonNode? currentNode)
    {
        switch (currentNode)
        {
            case JsonObject jsonObjectNode:
                _ = jsonObjectNode.Remove("$id");

                foreach (var property in jsonObjectNode.ToList())
                {
                    RemoveSchemaIds(property.Value);
                }

                break;

            case JsonArray jsonArrayNode:
                foreach (var arrayElement in jsonArrayNode)
                {
                    RemoveSchemaIds(arrayElement);
                }

                break;
        }
    }

    private void EscapeJsonReferencePointers(JsonNode? currentNode)
    {
        switch (currentNode)
        {
            case JsonObject jsonObjectNode:
                ProcessJsonObjectForEscaping(jsonObjectNode);
                break;

            case JsonArray jsonArrayNode:
                foreach (var arrayElement in jsonArrayNode)
                {
                    EscapeJsonReferencePointers(arrayElement);
                }

                break;
        }
    }

    private void ProcessJsonObjectForEscaping(JsonObject jsonObject)
    {
        var propertiesToRename = jsonObject
            .Select(property => property.Key)
            .Select(propertyName => (originalName: propertyName, strippedName: RemoveContextSuffix(propertyName)))
            .Where(namePair => namePair.strippedName != namePair.originalName)
            .ToList();

        foreach (var (originalName, strippedName) in propertiesToRename)
        {
            RenameJsonProperty(jsonObject, originalName, strippedName);
        }

        if (jsonObject.TryGetPropertyValue("required", out var requiredPropertiesNode) &&
            requiredPropertiesNode is JsonArray requiredPropertiesArray)
        {
            RemoveContextSuffixFromRequiredProperties(requiredPropertiesArray);
        }

        foreach (var (propertyName, propertyValue) in jsonObject.ToList())
        {
            if (propertyName == "$ref" &&
                propertyValue is JsonValue referenceValue &&
                referenceValue.TryGetValue<string>(out var referenceString))
            {
                if (referenceString.StartsWith(DefinitionsPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    jsonObject["$ref"] = BuildEscapedReferencePath(referenceString, DefinitionsPrefix);
                }
                else if (referenceString.StartsWith(DefsPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    jsonObject["$ref"] = BuildEscapedReferencePath(referenceString, DefsPrefix);
                }
            }
            else
            {
                EscapeJsonReferencePointers(propertyValue);
            }
        }
    }

    private string BuildEscapedReferencePath(string originalReferencePath, string prefix)
    {
        var referenceWithoutPrefix = originalReferencePath[prefix.Length..];
        var strippedReference = RemoveContextSuffix(referenceWithoutPrefix);
        var escapedReference = strippedReference
            .Replace("~", "~0", StringComparison.OrdinalIgnoreCase)
            .Replace("/", "~1", StringComparison.OrdinalIgnoreCase);

        return prefix + escapedReference;
    }

    private void RemoveContextSuffixFromRequiredProperties(JsonArray requiredProperties)
    {
        for (var index = 0; index < requiredProperties.Count; index++)
        {
            if (requiredProperties[index]?.GetValue<string>() is { } propertyName)
            {
                requiredProperties[index] = RemoveContextSuffix(propertyName);
            }
        }
    }

    private string RemoveContextSuffix(string propertyName)
    {
        var suffixIndex = propertyName.IndexOf(_contextPrefix, StringComparison.Ordinal);
        return suffixIndex >= 0 ? propertyName[..suffixIndex] : propertyName;
    }

    private static void RenameJsonProperty(JsonObject jsonObject, string oldPropertyName, string newPropertyName)
    {
        if (oldPropertyName == newPropertyName)
        {
            return;
        }

        var propertyValue = jsonObject[oldPropertyName];
        _ = jsonObject.Remove(oldPropertyName);
        jsonObject[newPropertyName] = propertyValue!;
    }
}

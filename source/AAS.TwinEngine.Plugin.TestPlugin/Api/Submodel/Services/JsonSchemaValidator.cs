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

    private static readonly JsonSerializerOptions Serialization = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public void ValidateResponseContent(string responseJson, JsonSchema requestSchema)
    {
        if (string.IsNullOrWhiteSpace(responseJson))
        {
            LogAndThrowException("Response JSON is empty.");
        }

        if (!TryParseJson(responseJson, out var responseDoc, out var parseError))
        {
            LogAndThrowException($"Failed to parse response JSON: {parseError}");
        }

        if (!TryNormalizeSchema(requestSchema, out var normalizedSchema, out var normalizeError))
        {
            LogAndThrowException($"Failed to normalize request schema: {normalizeError}");
        }

        if (!TryRegisterJsonSchema(normalizedSchema, out var registerError))
        {
            LogAndThrowException($"Failed to register schema: {registerError}");
        }

        try
        {
            var schema = JsonSchema.FromText(normalizedSchema.ToJsonString());
            var result = schema.Evaluate(responseDoc!.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
            if (!result.IsValid)
            {
                LogAndThrowException("Response did not validate against schema.");
            }
        }
        catch (Exception ex)
        {
            LogAndThrowException("Exception occurred during response validation.", ex);
        }
    }

    private void LogAndThrowException(string logMessage, Exception? ex = null)
    {
        if (ex != null)
        {
            logger.LogError(ex, logMessage);
        }
        else
        {
            logger.LogError(logMessage);
        }

        throw new NotFoundException(ExceptionMessages.ResourceNotValid);
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

    private bool TryNormalizeSchema(JsonSchema schema, out JsonObject normalized, out string? error)
    {
        error = null;
        normalized = [];

        try
        {
            var json = JsonSerializer.Serialize(schema, Serialization);

            normalized = JsonNode.Parse(json)?.AsObject()
            ?? throw new ArgumentException("Failed to parse schema JSON.");

            EscapeJsonReferencePointers(normalized);
            normalized["$id"] = normalized["$id"]?.GetValue<string>() ?? $"urn:uuid:{Guid.NewGuid():D}";

            return true;
        }
        catch (Exception ex)
        {
            error = $"Schema normalization failed: {ex.Message}";
            return false;
        }
    }

    private static bool TryRegisterJsonSchema(JsonObject schemaJsonObject, out string? registrationErrorMessage)
    {
        registrationErrorMessage = null;

        try
        {
            var jsonSchema = JsonSchema.FromText(schemaJsonObject.ToJsonString());
            var schemaIdentifierUri = new Uri(schemaJsonObject["$id"]!.GetValue<string>()!);
            SchemaRegistry.Global.Register(schemaIdentifierUri, jsonSchema);
            return true;
        }
        catch (Exception exception)
        {
            registrationErrorMessage = $"Schema registration failed: {exception.Message}";
            return false;
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

        foreach (var property in jsonObject.ToList())
        {
            var propertyName = property.Key;
            var propertyValue = property.Value;

            if (propertyName == "$ref" &&
                propertyValue is JsonValue referenceValue &&
                referenceValue.TryGetValue<string>(out var referenceString) &&
                referenceString.StartsWith(DefinitionsPrefix, StringComparison.OrdinalIgnoreCase))
            {
                jsonObject["$ref"] = BuildEscapedReferencePath(referenceString);
            }
            else
            {
                EscapeJsonReferencePointers(propertyValue);
            }
        }
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

    private string BuildEscapedReferencePath(string originalReferencePath)
    {
        var referenceWithoutPrefix = originalReferencePath[DefinitionsPrefix.Length..];

        var strippedReference = RemoveContextSuffix(referenceWithoutPrefix);

        var escapedReference = strippedReference.Replace("~", "~0", StringComparison.OrdinalIgnoreCase).Replace("/", "~1", StringComparison.OrdinalIgnoreCase);

        return DefinitionsPrefix + escapedReference;
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
        jsonObject.Remove(oldPropertyName);
        jsonObject[newPropertyName] = propertyValue!;
    }
}

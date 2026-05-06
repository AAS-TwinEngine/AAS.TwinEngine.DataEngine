using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;

using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.Submodel.Config;

using Json.Schema;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Services.Validation;

public class JsonSchemaNormalizer(IOptions<Semantics> semantics, JsonSchemaDraftSelector draftSelector) : IJsonSchemaNormalizer
{
    private readonly string _contextPrefix = semantics.Value.IndexContextPrefix;

    public bool TryNormalizeSchema(JsonSchema schema, string targetMetaSchemaId, [NotNullWhen(true)] out JsonSchema? normalizedSchema, out string? error)
    {
        error = null;
        normalizedSchema = null;

        try
        {
            var json = JsonSerializer.Serialize(schema, JsonSchemaValidationSerialization.Options);
            var normalized = JsonNode.Parse(json)?.AsObject();

            if (normalized == null)
            {
                throw new ArgumentException("Failed to parse schema JSON.", nameof(schema));
            }

            EscapeJsonReferencePointers(normalized);
            RemoveSchemaIds(normalized);
            normalized["$schema"] = targetMetaSchemaId;

            normalizedSchema = JsonSchema.FromText(normalized.ToJsonString());
            return true;
        }
        catch (JsonException ex)
        {
            error = $"Schema normalization failed: {ex.Message}";
            return false;
        }
        catch (NotSupportedException ex)
        {
            error = $"Schema normalization failed: {ex.Message}";
            return false;
        }
        catch (InvalidOperationException ex)
        {
            error = $"Schema normalization failed: {ex.Message}";
            return false;
        }
        catch (ArgumentException ex)
        {
            error = $"Schema normalization failed: {ex.Message}";
            return false;
        }
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

        if (jsonObject.TryGetPropertyValue("required", out var requiredPropertiesNode)
            && requiredPropertiesNode is JsonArray requiredPropertiesArray)
        {
            RemoveContextSuffixFromRequiredProperties(requiredPropertiesArray);
        }

        foreach (var (propertyName, propertyValue) in jsonObject.ToList())
        {
            if (propertyName == "$ref"
                && propertyValue is JsonValue referenceValue
                && referenceValue.TryGetValue<string>(out var referenceString))
            {
                var prefix = draftSelector
                    .GetKnownRefPrefixes()
                    .FirstOrDefault(knownPrefix => referenceString.StartsWith(knownPrefix, StringComparison.OrdinalIgnoreCase));

                if (prefix != null)
                {
                    jsonObject["$ref"] = BuildEscapedReferencePath(referenceString, prefix);
                    continue;
                }
            }

            EscapeJsonReferencePointers(propertyValue);
        }
    }

    private string BuildEscapedReferencePath(string originalReferencePath, string prefix)
    {
        var referenceWithoutPrefix = originalReferencePath[prefix.Length..];
        var strippedReference = RemoveContextSuffix(referenceWithoutPrefix);
        var escapedReference = strippedReference
            .Replace("~", "~0", StringComparison.Ordinal)
            .Replace("/", "~1", StringComparison.Ordinal);

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
        jsonObject[newPropertyName] = propertyValue;
    }
}

using System.Text.Json;
using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Helper.Validation;
using AAS.TwinEngine.DataEngine.Infrastructure.Shared;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Json.Schema;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Helper.Validation;

public sealed class JsonSchemaNormalizer(IOptions<PluginsConfig> pluginsConfig, JsonSchemaDraftSelector draftSelector, ILogger<JsonSchemaNormalizer> logger) : IJsonSchemaNormalizer
{
    private readonly string _contextPrefix = pluginsConfig.Value.SubmodelElementIndexContextPrefix;
    private readonly IReadOnlyList<string> _knownRefPrefixes = draftSelector.GetKnownRefPrefixes();

    public bool TryNormalizeSchema(
        JsonSchema schema,
        string targetMetaSchemaId,
        out JsonSchema normalizedSchema,
        out string? error)
    {
        error = null;
        normalizedSchema = null!;

        try
        {
            var json = JsonSerializer.Serialize(schema, JsonSerializationOptions.SerializationWithEnum);

            var normalized = JsonNode.Parse(json)?.AsObject();

            EscapeJsonReferencePointers(normalized);
            if (normalized == null)
            {
                throw new InvalidDependencyException(nameof(normalized), logger);
            }

            RemoveSchemaIds(normalized);
            normalized["$schema"] = targetMetaSchemaId;

            normalizedSchema = JsonSchema.FromText(normalized.ToJsonString());

            return true;
        }
        catch (Exception ex)
        {
            error = $"Schema normalization failed: {ex.Message}";
            return false;
        }
    }

    public void EscapeJsonReferencePointers(JsonNode? currentNode)
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
                _knownRefPrefixes.Any(prefix => referenceString.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
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
        var prefix = _knownRefPrefixes.First(p => originalReferencePath.StartsWith(p, StringComparison.OrdinalIgnoreCase));
        var referenceWithoutPrefix = originalReferencePath[prefix.Length..];

        var strippedReference = RemoveContextSuffix(referenceWithoutPrefix);

        var escapedReference = strippedReference.Replace("~", "~0", StringComparison.OrdinalIgnoreCase)
            .Replace("/", "~1", StringComparison.OrdinalIgnoreCase);

        return prefix + escapedReference;
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

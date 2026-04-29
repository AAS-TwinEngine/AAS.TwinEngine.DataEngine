using System.Text.Json;
using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Helper;
using AAS.TwinEngine.DataEngine.Infrastructure.Shared;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Json.Schema;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Helper;

public class JsonSchemaValidator(IOptions<PluginsConfig> pluginsConfig, ILogger<JsonSchemaValidator> logger) : IJsonSchemaValidator
{
    private readonly string _contextPrefix = pluginsConfig.Value.SubmodelElementIndexContextPrefix;
    private static readonly IReadOnlyList<string> KnownRefPrefixes = ["#/definitions/", "#/$defs/"];
    private static readonly JsonSchemaDraft Draft7 = new("Draft-07", MetaSchemas.Draft7Id.OriginalString, MetaSchemas.Draft7);
    private static readonly JsonSchemaDraft Draft202012 = new("Draft 2020-12", MetaSchemas.Draft202012Id.OriginalString, MetaSchemas.Draft202012);

    public void ValidateRequestSchema(JsonSchema schema)
    {
        if (schema == null)
        {
            LogAndThrowException("Requested schema is null.");
        }

        if (!TrySerializeSchema(schema!, out var schemaText, out var serializationError))
        {
            LogAndThrowException($"Schema serialization failed: {serializationError}");
        }

        if (!TryParseSchemaNode(schemaText, out var schemaNode, out var parseError))
        {
            LogAndThrowException($"Schema JSON is invalid: {parseError}");
        }

        if (schemaNode == null)
        {
            LogAndThrowException("Serialized schema resulted in null JsonNode.");
        }

        try
        {
            var metaSchema = ResolveMetaSchema(schemaNode);
            using var schemaDoc = JsonDocument.Parse(schemaNode!.ToJsonString());
            var result = metaSchema.Evaluate(schemaDoc.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
            if (!result.IsValid)
            {
                var details = TrySerializeEvaluationResult(result);
                LogAndThrowException($"Schema is not valid against the selected JSON Schema draft. Details: {details}");
            }
        }
        catch (Exception ex)
        {
            LogAndThrowException($"Meta-schema evaluation failed: {ex.Message}", ex);
        }
    }

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

        if (!TryNormalizeSchema(requestSchema, out var preparedSchema, out var normalizeError))
        {
            LogAndThrowException($"Failed to normalize request schema: {normalizeError}");
        }

        try
        {
            var result = preparedSchema.Evaluate(responseDoc!.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
            if (!result.IsValid)
            {
                var errorDetails = TrySerializeEvaluationResult(result);
                LogAndThrowException($"Response did not validate against schema. Details: {errorDetails}");
            }
        }
        catch (Exception ex)
        {
            LogAndThrowException($"Exception occurred during response validation: {ex.Message}", ex);
        }
    }

    private static JsonSchemaDraft ResolveDraftFromSchemaNode(JsonNode? schemaNode)
    {
        var declaredSchema = schemaNode?["$schema"]?.GetValue<string>();
        if (string.Equals(declaredSchema, Draft7.MetaSchemaId, StringComparison.OrdinalIgnoreCase))
        {
            return Draft7;
        }

        return Draft202012;
    }

    private static string TrySerializeEvaluationResult(EvaluationResults result)
    {
        try
        {
            return JsonSerializer.Serialize(result);
        }
        catch
        {
            return "Unable to serialize evaluation details.";
        }
    }

    private static JsonSchema ResolveMetaSchema(JsonNode? schemaNode)
    {
        var declaredSchema = schemaNode?["$schema"]?.GetValue<string>();
        if (string.Equals(declaredSchema, MetaSchemas.Draft7Id.OriginalString, StringComparison.OrdinalIgnoreCase))
        {
            return MetaSchemas.Draft7;
        }

        if (string.Equals(declaredSchema, MetaSchemas.Draft202012Id.OriginalString, StringComparison.OrdinalIgnoreCase))
        {
            return MetaSchemas.Draft202012;
        }

        return MetaSchemas.Draft202012;
    }

    private void LogAndThrowException(string logMessage, Exception? ex = null)
    {
        if (ex != null)
        {
            logger.LogError(ex, "{LogMessage}", logMessage);
        }
        else
        {
            logger.LogError("{LogMessage}", logMessage);
        }

        throw new InternalDataProcessingException();
    }

    private static bool TrySerializeSchema(JsonSchema schema, out string schemaText, out string? error)
    {
        error = null;
        schemaText = string.Empty;

        try
        {
            schemaText = JsonSerializer.Serialize(schema, JsonSerializationOptions.SerializationWithEnum);
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

    private bool TryNormalizeSchema(JsonSchema schema, out JsonSchema normalizedSchema, out string? error)
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

            var draft = ResolveDraftFromSchemaNode(normalized);
            RemoveSchemaIds(normalized);
            normalized["$schema"] = draft.MetaSchemaId;

            normalizedSchema = JsonSchema.FromText(normalized.ToJsonString());

            return true;
        }
        catch (Exception ex)
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
                KnownRefPrefixes.Any(p => referenceString.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
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
        var prefix = KnownRefPrefixes.First(p => originalReferencePath.StartsWith(p, StringComparison.OrdinalIgnoreCase));
        var referenceWithoutPrefix = originalReferencePath[prefix.Length..];

        var strippedReference = RemoveContextSuffix(referenceWithoutPrefix);

        var escapedReference = strippedReference.Replace("~", "~0", StringComparison.OrdinalIgnoreCase).Replace("/", "~1", StringComparison.OrdinalIgnoreCase);

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

    private sealed record JsonSchemaDraft(string Name, string MetaSchemaId, JsonSchema MetaSchema);
}

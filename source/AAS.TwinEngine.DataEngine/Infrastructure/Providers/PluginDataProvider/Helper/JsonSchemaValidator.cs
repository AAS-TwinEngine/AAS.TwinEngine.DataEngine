using System.Text.Json;
using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Helper;
using AAS.TwinEngine.DataEngine.Infrastructure.Shared;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Json.Schema;
using Json.Schema.Keywords;

using Microsoft.Extensions.Options;

public class JsonSchemaValidator(IOptions<PluginsConfig> pluginsConfig, ILogger<JsonSchemaValidator> logger) : IJsonSchemaValidator
{
    private readonly string _contextPrefix = pluginsConfig.Value.SubmodelElementIndexContextPrefix;

    private const string DefsPrefix = "#/$defs/";
    private const string DefinitionsPrefix = "#/definitions/";
    private const string Draft7Schema = "http://json-schema.org/draft-07/schema#";
    private const string Draft7SchemaHttps = "https://json-schema.org/draft-07/schema#";
    private const string Draft201909Schema = "https://json-schema.org/draft/2019-09/schema";
    private const string Draft202012Schema = "https://json-schema.org/draft/2020-12/schema";

    private readonly EvaluationOptions _evaluationOptions = new()
    {
        OutputFormat = OutputFormat.List
    };

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
            var normalizedSchema = schemaNode!.AsObject();
            var draftUri = ExtractDraftUri(normalizedSchema);
            var jsonElement = JsonDocument.Parse(normalizedSchema.ToJsonString()).RootElement;

            var result = ResolveMetaSchema(draftUri).Evaluate(jsonElement, _evaluationOptions);

            if (!result.IsValid)
            {
                LogAndThrowException($"Schema is not valid against '{draftUri}'.");
            }
        }
        catch (Exception ex)
        {
            LogAndThrowException("Schema dialect evaluation failed.", ex);
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

        if (!TryNormalizeSchema(requestSchema, out var normalizedSchema, out var normalizeError))
        {
            LogAndThrowException($"Failed to normalize request schema: {normalizeError}");
        }

        try
        {
            var schema = JsonSchema.FromText(normalizedSchema.ToJsonString());

            var result = schema.Evaluate(responseDoc!.RootElement, _evaluationOptions);

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

    private bool TryNormalizeSchema(JsonSchema schema, out JsonObject normalized, out string? error)
    {
        error = null;
        normalized = [];

        try
        {
            var json = JsonSerializer.Serialize(schema, JsonSerializationOptions.SerializationWithEnum);

            normalized = JsonNode.Parse(json)?.AsObject()
                ?? throw new InvalidDependencyException(nameof(normalized), logger);

            EscapeJsonReferencePointers(normalized);

            normalized["$schema"] = ExtractDraftUri(normalized);

            return true;
        }
        catch (Exception ex)
        {
            error = $"Schema normalization failed: {ex.Message}";
            return false;
        }
    }

    private void EscapeJsonReferencePointers(JsonNode? currentNode)
    {
        switch (currentNode)
        {
            case JsonObject obj:
                ProcessJsonObjectForEscaping(obj);
                break;

            case JsonArray array:
                foreach (var item in array)
                {
                    EscapeJsonReferencePointers(item);
                }
                break;
        }
    }

    private void ProcessJsonObjectForEscaping(JsonObject jsonObject)
    {
        var propertiesToRename = jsonObject
            .Select(p => p.Key)
            .Select(name => (original: name, stripped: RemoveContextSuffix(name)))
            .Where(x => x.original != x.stripped)
            .ToList();

        foreach (var (original, stripped) in propertiesToRename)
        {
            RenameJsonProperty(jsonObject, original, stripped);
        }

        if (jsonObject.TryGetPropertyValue("required", out var requiredNode) &&
            requiredNode is JsonArray requiredArray)
        {
            RemoveContextSuffixFromRequiredProperties(requiredArray);
        }

        foreach (var property in jsonObject.ToList())
        {
            if (property.Key == "$ref" &&
                property.Value is JsonValue value &&
                value.TryGetValue<string>(out var reference))
            {
                if (reference.StartsWith(DefsPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    jsonObject["$ref"] = BuildEscapedReferencePath(reference);
                }
                else if (reference.StartsWith(DefinitionsPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    jsonObject["$ref"] = BuildEscapedReferencePath(reference);
                }
            }
            else
            {
                EscapeJsonReferencePointers(property.Value);
            }
        }
    }

    private void RemoveContextSuffixFromRequiredProperties(JsonArray requiredProperties)
    {
        for (var i = 0; i < requiredProperties.Count; i++)
        {
            if (requiredProperties[i]?.GetValue<string>() is { } name)
            {
                requiredProperties[i] = RemoveContextSuffix(name);
            }
        }
    }

    private string BuildEscapedReferencePath(string reference)
    {
        if (reference.StartsWith(DefsPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return BuildEscapedReferencePath(reference, DefsPrefix);
        }

        if (reference.StartsWith(DefinitionsPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return BuildEscapedReferencePath(reference, DefinitionsPrefix);
        }

        return reference;
    }

    private string BuildEscapedReferencePath(string reference, string prefix)
    {
        if (!reference.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return reference;
        }

        var body = reference[prefix.Length..];

        var stripped = RemoveContextSuffix(body);

        var escaped = stripped
            .Replace("~", "~0", StringComparison.OrdinalIgnoreCase)
            .Replace("/", "~1", StringComparison.OrdinalIgnoreCase);

        return prefix + escaped;
    }

    private string RemoveContextSuffix(string propertyName)
    {
        var index = propertyName.IndexOf(_contextPrefix, StringComparison.Ordinal);
        return index >= 0 ? propertyName[..index] : propertyName;
    }

    private static void RenameJsonProperty(JsonObject jsonObject, string oldName, string newName)
    {
        if (oldName == newName)
        {
            return;
        }

        var value = jsonObject[oldName];
        _ = jsonObject.Remove(oldName);
        jsonObject[newName] = value!;
    }

    private static string ExtractDraftUri(JsonObject schema)
    {
        var raw = schema.TryGetPropertyValue("$schema", out var node) && node != null
            ? node.GetValue<string>().Trim()
            : null;

        return raw switch
        {
            Draft7Schema or Draft7SchemaHttps => Draft7Schema,
            Draft201909Schema => Draft201909Schema,
            Draft202012Schema => Draft202012Schema,
            _ => Draft202012Schema
        };
    }

    private static JsonSchema ResolveMetaSchema(string draftUri)
    {
        return draftUri switch
        {
            Draft7Schema => MetaSchemas.Draft7,
            Draft201909Schema => MetaSchemas.Draft201909,
            _ => MetaSchemas.Draft202012
        };
    }
}

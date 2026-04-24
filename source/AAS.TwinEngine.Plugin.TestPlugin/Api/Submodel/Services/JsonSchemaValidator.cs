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
    private const string DefsPrefix = "#/$defs/";

    private readonly EvaluationOptions _evaluationOptions = new()
    {
        OutputFormat = OutputFormat.List
    };

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

            normalized["$schema"] ??= "https://json-schema.org/draft/2020-12/schema";

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
            }
            else
            {
                EscapeJsonReferencePointers(property.Value);
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

    private string BuildEscapedReferencePath(string reference)
    {
        if (!reference.StartsWith(DefsPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return reference;
        }

        var body = reference[DefsPrefix.Length..];

        var stripped = RemoveContextSuffix(body);

        var escaped = stripped
            .Replace("~", "~0", StringComparison.OrdinalIgnoreCase)
            .Replace("/", "~1", StringComparison.OrdinalIgnoreCase);

        return DefsPrefix + escaped;
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

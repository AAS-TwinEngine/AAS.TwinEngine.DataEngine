using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;

using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Exceptions;

using Json.Schema;

namespace AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Services.Validation;

public class JsonSchemaValidator(
    JsonSchemaDraftSelector draftSelector,
    IJsonSchemaNormalizer schemaNormalizer,
    ILogger<JsonSchemaValidator> logger) : IJsonSchemaValidator
{
    private const string LogMessageTemplate = "{LogMessage}";

    public void ValidateRequestSchema(JsonSchema schema)
    {
        if (!TrySerializeSchema(schema, out var schemaText, out var serializationError))
        {
            LogAndThrowRequestException($"Schema serialization failed: {serializationError}");
        }

        if (!TryParseSchemaNode(schemaText, out var schemaNode, out var parseError))
        {
            LogAndThrowRequestException($"Schema JSON is invalid: {parseError}");
        }

        try
        {
            var declaredSchema = (schemaNode as JsonObject)?["$schema"]?.GetValue<string>();
            var draftHandler = draftSelector.Resolve(declaredSchema);
            using var schemaDocument = JsonDocument.Parse(schemaNode.ToJsonString());
            var result = draftHandler.MetaSchema.Evaluate(schemaDocument.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
            if (!result.IsValid)
            {
                var details = TrySerializeEvaluationResult(result);
                LogAndThrowRequestException($"Schema is not valid against the selected JSON Schema draft. Details: {details}");
            }
        }
        catch (JsonException ex)
        {
            LogAndThrowRequestException("JSON Schema evaluation failed.", ex);
        }
        catch (InvalidOperationException ex)
        {
            LogAndThrowRequestException("JSON Schema evaluation failed.", ex);
        }
        catch (ArgumentException ex)
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

        var declaredSchema = TryGetDeclaredSchema(requestSchema);
        var draftHandler = draftSelector.Resolve(declaredSchema);

        if (!schemaNormalizer.TryNormalizeSchema(requestSchema, draftHandler.MetaSchemaId, out var preparedSchema, out var normalizeError))
        {
            LogAndThrowResponseException($"Failed to normalize request schema: {normalizeError}");
        }

        try
        {
            var result = preparedSchema.Evaluate(responseDoc.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
            if (!result.IsValid)
            {
                var errorDetails = TrySerializeEvaluationResult(result);
                LogAndThrowResponseException($"Response did not validate against schema. Details: {errorDetails}");
            }
        }
        catch (JsonException ex)
        {
            LogAndThrowResponseException("Exception occurred during response validation.", ex);
        }
        catch (InvalidOperationException ex)
        {
            LogAndThrowResponseException("Exception occurred during response validation.", ex);
        }
        catch (ArgumentException ex)
        {
            LogAndThrowResponseException("Exception occurred during response validation.", ex);
        }
    }

    private static string? TryGetDeclaredSchema(JsonSchema schema)
    {
        if (!TrySerializeSchema(schema, out var schemaText, out _))
        {
            return null;
        }

        if (!TryParseSchemaNode(schemaText, out var schemaNode, out _))
        {
            return null;
        }

        return schemaNode?["$schema"]?.GetValue<string>();
    }

    private static string TrySerializeEvaluationResult(EvaluationResults result)
    {
        try
        {
            return JsonSerializer.Serialize(result);
        }
        catch (NotSupportedException)
        {
            return "Unable to serialize evaluation details.";
        }
        catch (InvalidOperationException)
        {
            return "Unable to serialize evaluation details.";
        }
    }

    [DoesNotReturn]
    private void LogAndThrowRequestException(string logMessage, Exception? ex = null)
    {
        if (ex != null)
        {
            logger.LogError(ex, LogMessageTemplate, logMessage);
        }
        else
        {
            logger.LogError(LogMessageTemplate, logMessage);
        }

        throw new BadRequestException(logMessage);
    }

    [DoesNotReturn]
    private void LogAndThrowResponseException(string logMessage, Exception? ex = null)
    {
        if (ex != null)
        {
            logger.LogError(ex, LogMessageTemplate, logMessage);
        }
        else
        {
            logger.LogError(LogMessageTemplate, logMessage);
        }

        throw new NotFoundException(logMessage);
    }

    private static bool TrySerializeSchema(JsonSchema schema, out string schemaText, out string? error)
    {
        error = null;
        schemaText = string.Empty;

        try
        {
            schemaText = JsonSerializer.Serialize(schema, JsonSchemaValidationSerialization.Options);
            return true;
        }
        catch (NotSupportedException ex)
        {
            error = $"Serialization failed: {ex.Message}";
            return false;
        }
        catch (InvalidOperationException ex)
        {
            error = $"Serialization failed: {ex.Message}";
            return false;
        }
    }

    private static bool TryParseSchemaNode(string schemaText, [NotNullWhen(true)] out JsonNode? node, out string? error)
    {
        error = null;
        node = null;

        try
        {
            node = JsonNode.Parse(schemaText);
            if (node == null)
            {
                error = "Schema JSON parsing returned null.";
                return false;
            }

            return true;
        }
        catch (JsonException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static bool TryParseJson(string json, [NotNullWhen(true)] out JsonDocument? document, out string? error)
    {
        error = null;
        document = null;

        try
        {
            document = JsonDocument.Parse(json);
            return true;
        }
        catch (JsonException ex)
        {
            error = $"JSON parsing failed: {ex.Message}";
            return false;
        }
    }
}

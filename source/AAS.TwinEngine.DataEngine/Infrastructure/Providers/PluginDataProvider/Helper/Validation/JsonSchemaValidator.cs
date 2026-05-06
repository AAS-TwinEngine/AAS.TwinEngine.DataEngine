using System.Text.Json;
using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Helper;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Helper.Validation;
using AAS.TwinEngine.DataEngine.Infrastructure.Shared;

using Json.Schema;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Helper.Validation;

public class JsonSchemaValidator(JsonSchemaDraftSelector draftSelector, IJsonSchemaNormalizer schemaNormalizer, ILogger<JsonSchemaValidator> logger) : IJsonSchemaValidator
{
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
            var declaredSchema = (schemaNode as JsonObject)?["$schema"]?.GetValue<string>();
            var draftHandler = draftSelector.Resolve(declaredSchema);
            using var schemaDoc = JsonDocument.Parse(schemaNode!.ToJsonString());
            var result = draftHandler.MetaSchema.Evaluate(schemaDoc.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
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

        var declaredSchema = TryGetDeclaredSchema(requestSchema);
        var draftHandler = draftSelector.Resolve(declaredSchema);

        if (!schemaNormalizer.TryNormalizeSchema(requestSchema, draftHandler.MetaSchemaId, out var preparedSchema, out var normalizeError))
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
        catch
        {
            return "Unable to serialize evaluation details.";
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
}

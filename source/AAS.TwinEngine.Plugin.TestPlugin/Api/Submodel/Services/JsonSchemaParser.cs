using System.Text.Json;
using System.Text.Json.Nodes;

using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Constants;
using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Exceptions;
using AAS.TwinEngine.Plugin.TestPlugin.DomainModel.Submodel;

using Json.Schema;

namespace AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Services;

public class JsonSchemaParser(ILogger<JsonSchemaParser> logger) : IJsonSchemaParser
{
    private const string Draft7Schema = "http://json-schema.org/draft-07/schema#";
    private const string Draft7SchemaHttps = "https://json-schema.org/draft-07/schema#";
    private const string Draft201909Schema = "https://json-schema.org/draft/2019-09/schema";
    private const string Draft202012Schema = "https://json-schema.org/draft/2020-12/schema";
    private const string DefsRefPrefix = "#/$defs/";
    private const string DefinitionsRefPrefix = "#/definitions/";

    private enum SchemaDraft { Draft7, Draft201909, Draft202012 }

    public SemanticTreeNode ParseJsonSchema(JsonSchema jsonSchema)
    {
        ValidateRequest(jsonSchema);
        return CreateSemanticTree(jsonSchema);
    }

    private void ValidateRequest(JsonSchema jsonSchema)
    {
        try
        {
            var json = JsonSerializer.SerializeToNode(jsonSchema);
            var schema = json?.AsObject() ?? throw new JsonException();
            var draftUri = GetSchemaDraftUri(schema);
            var element = JsonDocument.Parse(schema.ToJsonString()).RootElement;

            var result = ResolveMetaSchema(draftUri).Evaluate(element, new EvaluationOptions
            {
                OutputFormat = OutputFormat.List
            });

            if (!result.IsValid)
            {
                logger.LogError("Requested schema is not valid");
                throw new BadRequestException(ExceptionMessages.RequestBodyInvalid);
            }
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Requested schema is not valid");
            throw new BadRequestException(ExceptionMessages.FailedParsingJsonSchema);
        }
    }

    private SemanticTreeNode CreateSemanticTree(JsonSchema jsonSchema)
    {
        var json = JsonSerializer.SerializeToNode(jsonSchema)!.AsObject();
        var draft = GetSchemaDraft(json);

        if (!json.TryGetPropertyValue("properties", out var propsNode) ||
            propsNode is not JsonObject props ||
            props.Count == 0)
        {
            throw new BadRequestException(ExceptionMessages.InvalidJsonSchemaRootElement);
        }

        var rootProperty = ((IList<KeyValuePair<string, JsonNode?>>)props)[0];
        return ProcessProperty(rootProperty.Key, rootProperty.Value!, json, draft);
    }

    private SemanticTreeNode ProcessProperty(string name, JsonNode propertyNode, JsonObject root, SchemaDraft draft)
    {
        var property = propertyNode.AsObject();

        if (property.TryGetPropertyValue("$ref", out var refNode))
        {
            return HandleReference(name, refNode!.GetValue<string>(), root, draft);
        }

        var type = GetType(property);

        return type switch
        {
            DataType.Object => BuildObjectBranch(name, propertyNode, root, draft),
            DataType.Array => BuildArrayBranch(name, propertyNode, root, draft),
            _ => new SemanticLeafNode(name, type, "")
        };
    }

    private SemanticTreeNode HandleReference(string name, string reference, JsonObject root, SchemaDraft draft)
    {
        if (TryResolveReference(reference, root, draft, out var referenceNode))
        {
            return ProcessProperty(name, referenceNode!, root, draft);
        }

        return new SemanticLeafNode(name, DataType.Unknown, "");
    }

    private static bool TryResolveReference(string reference, JsonObject root, SchemaDraft draft, out JsonNode? referenceNode)
    {
        referenceNode = null;

        if (!TryGetReferenceKey(reference, out var key))
        {
            return false;
        }

        var preferred = GetPreferredDefinitionsProperty(draft);
        var fallback = preferred == "$defs" ? "definitions" : "$defs";

        if (TryGetDefinition(root, preferred, key, out referenceNode))
        {
            return true;
        }

        return TryGetDefinition(root, fallback, key, out referenceNode);
    }

    private static bool TryGetReferenceKey(string reference, out string key)
    {
        key = string.Empty;
        var prefix = GetReferencePrefix(reference);
        if (prefix == null) return false;
        key = DecodeJsonPointerToken(reference[prefix.Length..]);
        return !string.IsNullOrWhiteSpace(key);
    }

    private static string? GetReferencePrefix(string reference)
    {
        if (reference.StartsWith(DefsRefPrefix, StringComparison.OrdinalIgnoreCase)) return DefsRefPrefix;
        if (reference.StartsWith(DefinitionsRefPrefix, StringComparison.OrdinalIgnoreCase)) return DefinitionsRefPrefix;
        return null;
    }

    private static bool TryGetDefinition(JsonObject root, string definitionsProperty, string key, out JsonNode? defNode)
    {
        defNode = null;
        if (!root.TryGetPropertyValue(definitionsProperty, out var defsNode) || defsNode is not JsonObject defs)
            return false;
        return defs.TryGetPropertyValue(key, out defNode);
    }

    private SemanticBranchNode BuildObjectBranch(string name, JsonNode node, JsonObject root, SchemaDraft draft)
    {
        var obj = node.AsObject();
        var branch = new SemanticBranchNode(name, DataType.Object);

        if (obj.TryGetPropertyValue("properties", out var propsNode) &&
            propsNode is JsonObject props)
        {
            foreach (var prop in props)
            {
                branch.AddChild(ProcessProperty(prop.Key, prop.Value!, root, draft));
            }
        }

        return branch;
    }

    private SemanticBranchNode BuildArrayBranch(string name, JsonNode node, JsonObject root, SchemaDraft draft)
    {
        var obj = node.AsObject();
        var branch = new SemanticBranchNode(name, DataType.Array);

        if (!obj.TryGetPropertyValue("items", out var itemsNode) ||
            itemsNode is not JsonObject itemObj)
        {
            return branch;
        }

        var itemType = GetType(itemObj);

        if (itemType == DataType.Object &&
            itemObj.TryGetPropertyValue("properties", out var propsNode) &&
            propsNode is JsonObject props)
        {
            foreach (var prop in props)
            {
                branch.AddChild(ProcessProperty(prop.Key, prop.Value!, root, draft));
            }

            return branch;
        }

        if (itemObj.TryGetPropertyValue("$ref", out var refNode))
        {
            var resolved = HandleReference(name, refNode!.GetValue<string>(), root, draft);

            if (resolved is SemanticBranchNode refBranch)
            {
                foreach (var child in refBranch.Children)
                {
                    branch.AddChild(child);
                }
            }
            else
            {
                branch.AddChild(resolved);
            }

            return branch;
        }

        branch.AddChild(new SemanticLeafNode(name, itemType, ""));
        return branch;
    }

    private static DataType GetType(JsonObject obj)
    {
        if (!obj.TryGetPropertyValue("type", out var typeNode))
        {
            return DataType.String;
        }

        return typeNode!.ToString() switch
        {
            "object" => DataType.Object,
            "array" => DataType.Array,
            "string" => DataType.String,
            "integer" => DataType.Integer,
            "number" => DataType.Number,
            "boolean" => DataType.Boolean,
            _ => DataType.String
        };
    }

    private static SchemaDraft GetSchemaDraft(JsonObject schema)
    {
        return GetSchemaDraftUri(schema) switch
        {
            Draft7Schema or Draft7SchemaHttps => SchemaDraft.Draft7,
            Draft201909Schema => SchemaDraft.Draft201909,
            _ => SchemaDraft.Draft202012
        };
    }

    private static string GetPreferredDefinitionsProperty(SchemaDraft draft)
    {
        return draft == SchemaDraft.Draft7 ? "definitions" : "$defs";
    }

    private static string GetSchemaDraftUri(JsonObject schema)
    {
        if (!schema.TryGetPropertyValue("$schema", out var schemaNode) || schemaNode == null)
        {
            return Draft202012Schema;
        }

        var raw = schemaNode.GetValue<string>().Trim();

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

    private static string DecodeJsonPointerToken(string token)
    {
        return token
            .Replace("~1", "/", StringComparison.OrdinalIgnoreCase)
            .Replace("~0", "~", StringComparison.OrdinalIgnoreCase);
    }
}

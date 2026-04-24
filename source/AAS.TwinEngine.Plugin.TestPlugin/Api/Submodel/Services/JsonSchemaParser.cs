using System.Text.Json;
using System.Text.Json.Nodes;

using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Constants;
using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Exceptions;
using AAS.TwinEngine.Plugin.TestPlugin.DomainModel.Submodel;

using Json.Schema;

namespace AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Services;

public class JsonSchemaParser(ILogger<JsonSchemaParser> logger) : IJsonSchemaParser
{
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
            var element = JsonDocument.Parse(json!.ToJsonString()).RootElement;

            var result = MetaSchemas.Draft202012.Evaluate(element, new EvaluationOptions
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

        if (!json.TryGetPropertyValue("properties", out var propsNode) ||
            propsNode is not JsonObject props ||
            props.Count == 0)
        {
            throw new BadRequestException(ExceptionMessages.InvalidJsonSchemaRootElement);
        }

        var rootProperty = ((IList<KeyValuePair<string, JsonNode?>>)props)[0];
        return ProcessProperty(rootProperty.Key, rootProperty.Value!, json);
    }

    private SemanticTreeNode ProcessProperty(string name, JsonNode propertyNode, JsonObject root)
    {
        var property = propertyNode.AsObject();

        if (property.TryGetPropertyValue("$ref", out var refNode))
        {
            return HandleReference(name, refNode!.GetValue<string>(), root);
        }

        var type = GetType(property);

        return type switch
        {
            DataType.Object => BuildObjectBranch(name, propertyNode, root),
            DataType.Array => BuildArrayBranch(name, propertyNode, root),
            _ => new SemanticLeafNode(name, type, "")
        };
    }

    private SemanticTreeNode HandleReference(string name, string reference, JsonObject root)
    {
        if (!reference.StartsWith("#/$defs/", StringComparison.OrdinalIgnoreCase))
        {
            return new SemanticLeafNode(name, DataType.Unknown, "");
        }

        var key = reference.Replace("#/$defs/", "", StringComparison.OrdinalIgnoreCase);

        if (!root.TryGetPropertyValue("$defs", out var defsNode) ||
            defsNode is not JsonObject defs ||
            !defs.TryGetPropertyValue(key, out var defNode))
        {
            return new SemanticLeafNode(name, DataType.Unknown, "");
        }

        return ProcessProperty(name, defNode!, root);
    }

    private SemanticBranchNode BuildObjectBranch(string name, JsonNode node, JsonObject root)
    {
        var obj = node.AsObject();
        var branch = new SemanticBranchNode(name, DataType.Object);

        if (obj.TryGetPropertyValue("properties", out var propsNode) &&
            propsNode is JsonObject props)
        {
            foreach (var prop in props)
            {
                branch.AddChild(ProcessProperty(prop.Key, prop.Value!, root));
            }
        }

        return branch;
    }

    private SemanticBranchNode BuildArrayBranch(string name, JsonNode node, JsonObject root)
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
                branch.AddChild(ProcessProperty(prop.Key, prop.Value!, root));
            }

            return branch;
        }

        if (itemObj.TryGetPropertyValue("$ref", out var refNode))
        {
            var resolved = HandleReference(name, refNode!.GetValue<string>(), root);

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
}

using System.Text.Json;

using Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Constants;
using Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Exceptions;
using Aas.TwinEngine.Plugin.TestPlugin.DomainModel.Submodel;

using Json.Schema;

namespace Aas.TwinEngine.Plugin.TestPlugin.Api.Submodel.Services;

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
            var node = JsonSerializer.SerializeToNode(jsonSchema);
            var result = MetaSchemas.Draft7.Evaluate(node, new EvaluationOptions { OutputFormat = OutputFormat.List });
            if (!result.IsValid)
            {
                logger.LogError("Requested schema is not validate");
                throw new BadRequestException(ExceptionMessages.RequestBodyInvalid);
            }
        }
        catch (JsonException)
        {
            logger.LogError("Requested schema is not validate");
            throw new BadRequestException(ExceptionMessages.FailedParsingJsonSchema);
        }
    }

    private SemanticTreeNode CreateSemanticTree(JsonSchema jsonSchema)
    {
        var propertiesKeyword = jsonSchema.GetKeyword<PropertiesKeyword>();
        if (propertiesKeyword == null || !propertiesKeyword.Properties.Any())
        {
            throw new BadRequestException(ExceptionMessages.InvalidJsonSchemaRootElement);
        }

        var rootProperty = propertiesKeyword.Properties.First();
        return ProcessProperty(rootProperty.Key, rootProperty.Value, jsonSchema.GetKeyword<DefinitionsKeyword>());
    }

    private SemanticTreeNode ProcessProperty(string schemaPropertyName, JsonSchema property, DefinitionsKeyword definitions)
    {
        var refKeyword = property.GetKeyword<RefKeyword>();
        if (refKeyword != null)
        {
            return HandleReference(schemaPropertyName, property, definitions);
        }

        var typeKeyword = property.GetKeyword<TypeKeyword>();
        if (typeKeyword == null)
        {
            return new SemanticLeafNode(schemaPropertyName, DataType.String, "");
        }

        var schemaType = GetSchemaType(typeKeyword);
        if (schemaType is DataType.Object or DataType.Array)
        {
            return BuildObjectNode(schemaPropertyName, schemaType, property, definitions);
        }

        return new SemanticLeafNode(schemaPropertyName, schemaType, "");
    }

    private SemanticTreeNode HandleReference(string schemaPropertyName, JsonSchema property, DefinitionsKeyword definitions)
    {
        var refKeyword = property.GetKeyword<RefKeyword>();
        if (refKeyword == null)
        {
            return new SemanticLeafNode(schemaPropertyName, DataType.String, "");
        }

        var definitionKey = refKeyword.Reference.ToString().Replace("#/definitions/", "");
        if (definitions == null || !definitions.Definitions.TryGetValue(definitionKey, out var def))
        {
            return new SemanticLeafNode(schemaPropertyName, DataType.Unknown, "");
        }

        var defTypeKeyword = def.GetKeyword<TypeKeyword>();
        if (defTypeKeyword == null)
        {
            return new SemanticLeafNode(schemaPropertyName, DataType.String, "");
        }

        var schemaType = GetSchemaType(defTypeKeyword);
        if (schemaType is DataType.Object or DataType.Array)
        {
            return BuildObjectNode(schemaPropertyName, schemaType, def, definitions);
        }

        return new SemanticLeafNode(schemaPropertyName, schemaType, "");
    }

    private SemanticBranchNode BuildObjectNode(string schemaPropertyName, DataType dataType, JsonSchema schema, DefinitionsKeyword definitions)
    {
        var branchNode = new SemanticBranchNode(schemaPropertyName, dataType);

        switch (dataType)
        {
            case DataType.Object:
                {
                    var propertiesKeyword = schema.GetKeyword<PropertiesKeyword>();
                    if (propertiesKeyword != null)
                    {
                        foreach (var prop in propertiesKeyword.Properties)
                        {
                            branchNode.AddChild(ProcessProperty(prop.Key, prop.Value, definitions));
                        }
                    }

                    break;
                }
            case DataType.Array:
                {
                    var itemsKeyword = schema.GetKeyword<ItemsKeyword>();
                    if (itemsKeyword == null)
                    {
                        var propertiesKeyword = schema.GetKeyword<PropertiesKeyword>();
                        if (propertiesKeyword != null)
                        {
                            foreach (var prop in propertiesKeyword.Properties)
                            {
                                branchNode.AddChild(ProcessProperty(prop.Key, prop.Value, definitions));
                            }
                        }

                        break;
                    }

                    if (itemsKeyword is { SingleSchema: not null })
                    {
                        branchNode.AddChild(ProcessProperty("item", itemsKeyword.SingleSchema, definitions));
                    }

                    break;
                }
        }

        return branchNode;
    }

    private static DataType GetSchemaType(TypeKeyword typeKeyword)
    {
        var t = typeKeyword.Type;

        return t switch
        {
            _ when t.HasFlag(SchemaValueType.Object) => DataType.Object,
            _ when t.HasFlag(SchemaValueType.Array) => DataType.Array,
            _ when t.HasFlag(SchemaValueType.String) => DataType.String,
            _ when t.HasFlag(SchemaValueType.Integer) => DataType.Integer,
            _ when t.HasFlag(SchemaValueType.Number) => DataType.Number,
            _ when t.HasFlag(SchemaValueType.Boolean) => DataType.Boolean,
            _ => DataType.String
        };
    }

}

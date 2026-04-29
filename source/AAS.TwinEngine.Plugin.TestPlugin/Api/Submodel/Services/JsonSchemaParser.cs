using System.Text.Json;

using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Exceptions;
using AAS.TwinEngine.Plugin.TestPlugin.DomainModel.Submodel;

using Json.Schema;

namespace AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Services;

public class JsonSchemaParser(ILogger<JsonSchemaParser> logger, IJsonSchemaValidator jsonSchemaValidator) : IJsonSchemaParser
{
    private const string DefinitionsPath = "#/definitions/";
    private const string DefsPath = "#/$defs/";

    public SemanticTreeNode ParseJsonSchema(JsonSchema jsonSchema)
    {
        jsonSchemaValidator.ValidateRequestSchema(jsonSchema);

        return BuildSemanticTree(GetRootElement(jsonSchema));
    }

    private static SemanticTreeNode BuildSemanticTree(JsonElement schemaRoot)
    {
        if (!TryGetProperties(schemaRoot, out var propertiesElement))
        {
            throw new BadRequestException("Schema must contain at least one property.");
        }

        var rootProperty = propertiesElement.EnumerateObject().First();
        var definitions = GetDefinitionsElement(schemaRoot);

        return ConvertPropertyToNode(rootProperty.Name, rootProperty.Value, definitions);
    }

    private static SemanticTreeNode ConvertPropertyToNode(string propertyName, JsonElement propertySchema, JsonElement? definitions)
    {
        if (TryGetReference(propertySchema, out var reference))
        {
            return ResolveReference(propertyName, reference, definitions);
        }

        if (!TryMapSchemaTypeToDataType(propertySchema, out var dataType))
        {
            return CreateLeafNode(propertyName, DataType.String);
        }

        if (dataType == DataType.Array
            && TryGetPrimitiveArrayItemType(propertySchema, definitions, out var primitiveItemType))
        {
            return CreateLeafNode(propertyName, primitiveItemType);
        }

        if (IsComplexType(dataType))
        {
            return CreateBranchNode(propertyName, dataType, propertySchema, definitions);
        }

        return CreateLeafNode(propertyName, dataType);
    }

    private static SemanticTreeNode ResolveReference(string propertyName, string schemaReference, JsonElement? definitions)
    {
        var definitionKey = ExtractDefinitionKey(schemaReference);

        if (!TryGetDefinition(definitions, definitionKey, out var definitionSchema))
        {
            return CreateLeafNode(propertyName, DataType.Unknown);
        }

        if (!TryMapSchemaTypeToDataType(definitionSchema, out var dataType))
        {
            return CreateLeafNode(propertyName, DataType.String);
        }

        if (dataType == DataType.Array
            && TryGetPrimitiveArrayItemType(definitionSchema, definitions, out var primitiveItemType))
        {
            return CreateLeafNode(propertyName, primitiveItemType);
        }

        if (IsComplexType(dataType))
        {
            return CreateBranchNode(propertyName, dataType, definitionSchema, definitions);
        }

        return CreateLeafNode(propertyName, dataType);
    }

    private static string ExtractDefinitionKey(string schemaReference)
    {
        if (schemaReference.StartsWith(DefinitionsPath, StringComparison.Ordinal))
        {
            return schemaReference[DefinitionsPath.Length..];
        }

        if (schemaReference.StartsWith(DefsPath, StringComparison.Ordinal))
        {
            return schemaReference[DefsPath.Length..];
        }

        return schemaReference;
    }

    private static bool TryGetDefinition(JsonElement? definitions, string definitionKey, out JsonElement definitionSchema)
    {
        definitionSchema = default;

        if (!definitions.HasValue || definitions.Value.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!definitions.Value.TryGetProperty(definitionKey, out var found))
        {
            return false;
        }

        definitionSchema = found;
        return true;
    }

    private static SemanticBranchNode CreateBranchNode(string propertyName, DataType dataType, JsonElement schema, JsonElement? definitions)
    {
        var branchNode = new SemanticBranchNode(propertyName, dataType);

        switch (dataType)
        {
            case DataType.Object:
                AddChildPropertiesFromObject(branchNode, schema, definitions);
                break;

            case DataType.Array:
                AddChildPropertiesFromArray(branchNode, schema, definitions);
                break;
        }

        return branchNode;
    }

    private static void AddChildPropertiesFromObject(SemanticBranchNode parentBranch, JsonElement schema, JsonElement? definitions)
    {
        if (!TryGetProperties(schema, out var propertiesElement))
        {
            return;
        }

        foreach (var property in propertiesElement.EnumerateObject())
        {
            var childNode = ConvertPropertyToNode(property.Name, property.Value, definitions);
            parentBranch.AddChild(childNode);
        }
    }

    private static void AddChildPropertiesFromArray(SemanticBranchNode parentBranch, JsonElement schema, JsonElement? definitions)
    {
        if (!schema.TryGetProperty("items", out var itemsElement))
        {
            AddChildPropertiesFromObject(parentBranch, schema, definitions);
            return;
        }

        if (TryAddChildrenFromObjectItems(parentBranch, itemsElement, definitions))
        {
            return;
        }

        if (TryAddChildrenFromArrayItems(parentBranch, itemsElement, definitions))
        {
            return;
        }

        AddChildPropertiesFromObject(parentBranch, schema, definitions);
    }

    private static bool TryAddChildrenFromObjectItems(SemanticBranchNode parentBranch, JsonElement itemsElement, JsonElement? definitions)
    {
        if (itemsElement.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (itemsElement.TryGetProperty("properties", out var itemPropertiesElement)
            && itemPropertiesElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in itemPropertiesElement.EnumerateObject())
            {
                var childNode = ConvertPropertyToNode(property.Name, property.Value, definitions);
                parentBranch.AddChild(childNode);
            }

            return true;
        }

        AddItemNodeOrFlatten(parentBranch, ConvertPropertyToNode("item", itemsElement, definitions));
        return true;
    }

    private static bool TryAddChildrenFromArrayItems(SemanticBranchNode parentBranch, JsonElement itemsElement, JsonElement? definitions)
    {
        if (itemsElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var index = 0;
        foreach (var itemSchema in itemsElement.EnumerateArray())
        {
            if (itemSchema.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var itemNode = ConvertPropertyToNode($"item{index}", itemSchema, definitions);
            AddItemNodeOrFlatten(parentBranch, itemNode);
            index++;
        }

        return index > 0;
    }

    private static void AddItemNodeOrFlatten(SemanticBranchNode parentBranch, SemanticTreeNode itemNode)
    {
        if (itemNode is SemanticBranchNode itemBranch)
        {
            foreach (var child in itemBranch.Children)
            {
                parentBranch.AddChild(child);
            }

            return;
        }

        parentBranch.AddChild(itemNode);
    }

    private static SemanticLeafNode CreateLeafNode(string propertyName, DataType dataType) => new(propertyName, dataType, string.Empty);

    private static bool IsComplexType(DataType dataType) => dataType is DataType.Object or DataType.Array;

    private static JsonElement GetRootElement(JsonSchema schema)
    {
        if (schema.Root.Source.ValueKind != JsonValueKind.Undefined)
        {
            return schema.Root.Source;
        }

        var schemaText = JsonSerializer.Serialize(schema);
        using var document = JsonDocument.Parse(schemaText);
        return document.RootElement.Clone();
    }

    private static JsonElement? GetDefinitionsElement(JsonElement schema)
    {
        if (schema.TryGetProperty("definitions", out var definitionsElement) && definitionsElement.ValueKind == JsonValueKind.Object)
        {
            return definitionsElement;
        }

        if (schema.TryGetProperty("$defs", out var defsElement) && defsElement.ValueKind == JsonValueKind.Object)
        {
            return defsElement;
        }

        return null;
    }

    private static bool TryGetReference(JsonElement schema, out string reference)
    {
        reference = string.Empty;

        if (!schema.TryGetProperty("$ref", out var refElement) || refElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        reference = refElement.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(reference);
    }

    private static bool TryGetProperties(JsonElement schema, out JsonElement properties)
    {
        return schema.TryGetProperty("properties", out properties)
               && properties.ValueKind == JsonValueKind.Object
               && properties.EnumerateObject().Any();
    }

    private static bool TryMapSchemaTypeToDataType(JsonElement schema, out DataType dataType)
    {
        dataType = DataType.String;

        if (schema.TryGetProperty("type", out var typeElement) && TryMapTypeElement(typeElement, out dataType))
        {
            return true;
        }

        if (schema.TryGetProperty("properties", out var propertiesElement) && propertiesElement.ValueKind == JsonValueKind.Object)
        {
            dataType = DataType.Object;
            return true;
        }

        if (schema.TryGetProperty("items", out var itemsElement)
            && (itemsElement.ValueKind == JsonValueKind.Object || itemsElement.ValueKind == JsonValueKind.Array))
        {
            dataType = DataType.Array;
            return true;
        }

        return false;
    }

    private static bool TryMapTypeElement(JsonElement typeElement, out DataType dataType)
    {
        dataType = DataType.String;

        if (typeElement.ValueKind == JsonValueKind.String)
        {
            return TryMapTypeName(typeElement.GetString(), out dataType);
        }

        if (typeElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var typeNameElement in typeElement.EnumerateArray())
        {
            if (typeNameElement.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var typeName = typeNameElement.GetString();

            if (string.Equals(typeName, "null", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (TryMapTypeName(typeName, out dataType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryMapTypeName(string? schemaType, out DataType dataType)
    {
        dataType = DataType.String;

        if (string.IsNullOrWhiteSpace(schemaType))
        {
            return false;
        }

        switch (schemaType)
        {
            case "object":
                dataType = DataType.Object;
                return true;
            case "array":
                dataType = DataType.Array;
                return true;
            case "string":
                dataType = DataType.String;
                return true;
            case "integer":
                dataType = DataType.Integer;
                return true;
            case "number":
                dataType = DataType.Number;
                return true;
            case "boolean":
                dataType = DataType.Boolean;
                return true;
            default:
                return false;
        }
    }

    private static bool TryGetPrimitiveArrayItemType(JsonElement arraySchema, JsonElement? definitions, out DataType itemType)
    {
        itemType = DataType.String;

        if (!arraySchema.TryGetProperty("items", out var itemsElement))
        {
            return false;
        }

        if (itemsElement.ValueKind == JsonValueKind.Object)
        {
            return TryGetPrimitiveTypeFromSchema(itemsElement, definitions, out itemType);
        }

        if (itemsElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var itemSchema in itemsElement.EnumerateArray())
        {
            if (itemSchema.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (TryGetPrimitiveTypeFromSchema(itemSchema, definitions, out itemType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryGetPrimitiveTypeFromSchema(JsonElement schema, JsonElement? definitions, out DataType dataType)
    {
        dataType = DataType.String;

        if (TryGetReference(schema, out var schemaReference))
        {
            var definitionKey = ExtractDefinitionKey(schemaReference);
            if (!TryGetDefinition(definitions, definitionKey, out var definitionSchema))
            {
                return false;
            }

            return TryGetPrimitiveTypeFromSchema(definitionSchema, definitions, out dataType);
        }

        if (!TryMapSchemaTypeToDataType(schema, out dataType))
        {
            return false;
        }

        return dataType is not DataType.Object and not DataType.Array;
    }
}

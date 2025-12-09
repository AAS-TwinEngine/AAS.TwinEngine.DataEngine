using System.Text.Json;

using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Helper;

public static class JsonSchemaParser
{
    public static SemanticTreeNode ParseJsonSchema(string content)
    {
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.String)
        {
            return ConvertJsonElement(root);
        }

        var jsonString = root.GetString();
        using var nestedDoc = JsonDocument.Parse(jsonString!);
        return ConvertJsonElement(nestedDoc.RootElement);
    }

    private static SemanticTreeNode ConvertJsonElement(JsonElement element)
    {
        var properties = element.EnumerateObject().ToList();

        if (properties.Count == 0)
        {
            return new SemanticBranchNode(string.Empty, Cardinality.Unknown);
        }

        if (properties is [{ Value.ValueKind: JsonValueKind.String } _])
        {
            return new SemanticLeafNode(properties[0].Name, properties[0].Value.ToString(), DataType.Unknown, Cardinality.Unknown);
        }

        var rootProperty = element.EnumerateObject().First();

        var rootBranch = new SemanticBranchNode(rootProperty.Name, Cardinality.Unknown);

        ProcessJsonValue(rootProperty.Value, rootBranch);

        return rootBranch;
    }

    private static void ProcessJsonValue(JsonElement valueElement, SemanticBranchNode parentBranch)
    {
        switch (valueElement.ValueKind)
        {
            case JsonValueKind.Object:
                ProcessJsonObject(valueElement, parentBranch);
                break;

            case JsonValueKind.Array:
                ProcessJsonArray(valueElement, parentBranch);
                break;

            default:
                parentBranch.AddChild(new SemanticLeafNode(
                    parentBranch.SemanticId,
                    valueElement.ToString(), DataType.Unknown, Cardinality.Unknown
                ));
                break;
        }
    }

    private static void ProcessJsonObject(JsonElement objectElement, SemanticBranchNode parentBranch)
    {
        foreach (var property in objectElement.EnumerateObject())
        {
            if (IsPrimitiveValue(property.Value))
            {
                parentBranch.AddChild(new SemanticLeafNode(
                    property.Name,
                    property.Value.ToString(), DataType.Unknown, Cardinality.Unknown
                ));
            }
            else if (property.Value.ValueKind == JsonValueKind.Array)
            {
                var baseSemanticId = property.Name;
                ProcessJsonArray(property.Value, parentBranch, baseSemanticId);
            }
            else
            {
                var branch = new SemanticBranchNode(property.Name, Cardinality.Unknown);
                ProcessJsonValue(property.Value, branch);
                parentBranch.AddChild(branch);
            }
        }
    }

    private static void ProcessJsonArray(JsonElement arrayElement, SemanticBranchNode parentBranch, string? baseSemanticId = null)
    {
        foreach (var item in arrayElement.EnumerateArray())
        {
            var semanticId = baseSemanticId ?? parentBranch.SemanticId;
            var arrayItemBranch = new SemanticBranchNode(semanticId, Cardinality.Unknown);
            ProcessJsonValue(item, arrayItemBranch);
            parentBranch.AddChild(arrayItemBranch);
        }
    }

    private static bool IsPrimitiveValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String or
            JsonValueKind.Number or
            JsonValueKind.True or
            JsonValueKind.False or
            JsonValueKind.Null => true,
            _ => false
        };
    }
}

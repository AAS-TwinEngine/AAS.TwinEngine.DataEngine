using System.Text.Json;

using Aas.TwinEngine.Plugin.TestPlugin.DomainModel.Submodel;

namespace Aas.TwinEngine.Plugin.TestPlugin.Infrastructure.Providers.ManifestProvider.Helper;

public static class JsonConverter
{
    public static SemanticTreeNode ParseJson(JsonDocument doc)
    {
        ArgumentNullException.ThrowIfNull(doc);

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
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                {
                    var properties = element.EnumerateObject();
                    var propertyCount = properties.Count();

                    switch (propertyCount)
                    {
                        case 0:
                            return new SemanticBranchNode(string.Empty, GetDataType(element));
                        case 1:
                            {
                                var rootProperty = properties.First();
                                var rootBranch = new SemanticBranchNode(rootProperty.Name, GetDataType(rootProperty.Value));
                                ProcessJsonValue(rootProperty.Value, rootBranch);
                                return rootBranch;
                            }
                    }

                    var root = new SemanticBranchNode(string.Empty, GetDataType(element));
                    ProcessJsonObject(element, root);
                    return root;
                }
            case JsonValueKind.Array:
                {
                    var syntheticRoot = new SemanticBranchNode(string.Empty, GetDataType(element));
                    ProcessJsonArray(element, syntheticRoot);
                    return syntheticRoot;
                }
            default:
                return new SemanticLeafNode(string.Empty, GetDataType(element), element.ToString());
        }
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
                    GetDataType(valueElement),
                    valueElement.ToString()
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
                    GetDataType(property.Value),
                     property.Value.ToString()
                ));
            }
            else if (property.Value.ValueKind == JsonValueKind.Array)
            {
                var baseSemanticId = property.Name;
                ProcessJsonArray(property.Value, parentBranch, baseSemanticId);
            }
            else
            {
                var branch = new SemanticBranchNode(property.Name, GetDataType(property.Value));
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
            var arrayItemBranch = new SemanticBranchNode(semanticId, GetDataType(item));
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

    private static DataType GetDataType(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.Object => DataType.Object,
            JsonValueKind.Array => DataType.Array,
            JsonValueKind.String => DataType.String,
            JsonValueKind.Number => DataType.Number,
            JsonValueKind.True => DataType.Boolean,
            JsonValueKind.False => DataType.Boolean,
            JsonValueKind.Null => DataType.Null,
            JsonValueKind.Undefined => DataType.Unknown,
            _ => DataType.Unknown
        };
}

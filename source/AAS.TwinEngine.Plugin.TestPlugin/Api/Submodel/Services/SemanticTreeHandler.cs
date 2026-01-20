using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

using Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Constants;
using Aas.TwinEngine.Plugin.TestPlugin.DomainModel.Submodel;

using Json.Schema;

namespace Aas.TwinEngine.Plugin.TestPlugin.Api.Submodel.Services;

public class SemanticTreeHandler(IJsonSchemaValidator jsonSchemaValidator) : ISemanticTreeHandler
{
    public JsonObject GetJson(SemanticTreeNode semanticTreeNodeWithValues, JsonSchema dataQuery)
    {
        var nodeObject = ConvertNode(semanticTreeNodeWithValues);

        var convertedJsonObject = new JsonObject { [semanticTreeNodeWithValues.SemanticId] = nodeObject };

        var convertedJsonString = JsonSerializer.Serialize(convertedJsonObject);

        jsonSchemaValidator.ValidateResponseContent(convertedJsonString, dataQuery);

        return convertedJsonObject;
    }

    private static JsonNode ConvertNode(SemanticTreeNode treeNode)
    {
        return treeNode switch
        {
            SemanticLeafNode leaf => ConvertLeafValue(leaf),
            SemanticBranchNode branch => ConvertBranchNode(branch),
            _ => throw new ArgumentException(ExceptionMessages.UnknownTypeError),
        };
    }

    private static JsonNode ConvertLeafValue(SemanticLeafNode leafNode)
    {
        return leafNode.DataType switch
        {
            DataType.Boolean => TryParseBoolean(leafNode.Value),
            DataType.Integer => TryParseInteger(leafNode.Value),
            DataType.Number => TryParseNumber(leafNode.Value),
            DataType.String => JsonValue.Create(leafNode.Value),
            _ => JsonValue.Create(leafNode.Value)
        };
    }

    private static JsonNode TryParseBoolean(string text)
    {
        return bool.TryParse(text, out var result)
            ? JsonValue.Create(result)
            : JsonValue.Create(text);
    }

    private static JsonNode TryParseInteger(string text)
    {
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? JsonValue.Create(result)
            : JsonValue.Create(text);
    }

    private static JsonNode TryParseNumber(string text)
    {
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
            ? JsonValue.Create(result)
            : JsonValue.Create(text);
    }

    private static JsonNode ConvertBranchNode(SemanticBranchNode branchNode)
    {
        var isArray = branchNode.DataType == DataType.Array;
        var allBranchNodes = branchNode.Children.All(child => child is SemanticBranchNode);
        var allLeafNodes = branchNode.Children.All(child => child is SemanticLeafNode);
        var sameSemanticId = branchNode.Children.Select(child => child.SemanticId).Distinct().Count() == 1;
        var sameSematicIdAsBranch = branchNode.Children.All(child => child is SemanticLeafNode) && branchNode.Children.All(child => child.SemanticId == branchNode.SemanticId);
        var singleNode = branchNode.Children.Count() == 1;

        if (isArray)
        {
            var elementArray = new JsonArray();

            if (allBranchNodes && sameSemanticId && !singleNode)
            {
                foreach (var childBranch in branchNode.Children.Cast<SemanticBranchNode>())
                {
                    elementArray.Add(ConvertNode(childBranch));
                }

                return elementArray;
            }

            if (allLeafNodes && sameSemanticId && !singleNode)
            {
                foreach (var leaf in branchNode.Children.Cast<SemanticLeafNode>())
                {
                    elementArray.Add(BuildObjectFromLeafNode(leaf));
                }

                return elementArray;
            }

            var singleObject = BuildObjectFromChildren(branchNode.Children);
            elementArray.Add(singleObject);
            return elementArray;
        }

        return BuildObjectFromChildren(branchNode.Children);
    }

    private static JsonObject BuildObjectFromLeafNode(SemanticLeafNode leafNode)
    {
        var jsonObject = new JsonObject
        {
            [leafNode.SemanticId] = ConvertLeafValue(leafNode)
        };
        return jsonObject;
    }

    private static JsonObject BuildObjectFromChildren(IEnumerable<SemanticTreeNode> childNode)
    {
        var jsonObject = new JsonObject();

        var groups = childNode.GroupBy(child => child.SemanticId);

        foreach (var group in groups)
        {
            var convertedValues = group.Select(ConvertNode).ToList();
            var count = convertedValues.Count;
            var allArrays = convertedValues.All(v => v is JsonArray);
            var singleValue = count == 1;

            if (singleValue)
            {
                jsonObject[group.Key] = convertedValues[0];
            }
            else if (allArrays)
            {
                var mergedArray = new JsonArray();
                foreach (var array in convertedValues.Cast<JsonArray>())
                {
                    foreach (var element in array)
                    {
                        mergedArray.Add(element.DeepClone());
                    }
                }

                jsonObject[group.Key] = mergedArray;
            }
            else
            {
                var wrapperArray = new JsonArray();
                foreach (var valueNode in convertedValues)
                {
                    wrapperArray.Add(valueNode.DeepClone());
                }

                jsonObject[group.Key] = wrapperArray;
            }
        }

        return jsonObject;
    }
}


using System.Text.Json;
using System.Text.Json.Nodes;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Shared;

public static class AasJsonNodeDeserializer
{
    public static JsonNode? UnwrapResult(JsonNode? rootNode) => rootNode?["result"] ?? rootNode;

    public static T? DeserializeAasNode<T>(JsonNode? node, Func<JsonNode, T?> deserializer)
        where T : class
        => node is null ? null : deserializer(node);

    public static List<T>? DeserializeAasArray<T>(JsonNode? node, Func<JsonNode, T?> deserializer)
        where T : class
    {
        if (node is not JsonArray array)
        {
            return null;
        }

        return [.. array.Select(item => item is null ? null : deserializer(item))
                    .OfType<T>()];
    }

    public static T? DeserializeObject<T>(JsonNode? node, JsonSerializerOptions? options = null)
    {
        if (node is null)
        {
            return default;
        }

        return options is null
            ? node.Deserialize<T>(JsonSerializationOptions.DeserializationOption)
            : node.Deserialize<T>(options);
    }

    public static TEnum? DeserializeEnum<TEnum>(JsonNode? node)
        where TEnum : struct, Enum
    {
        if (node is null)
        {
            return null;
        }

        var value = node.GetValue<string>();
        return Enum.TryParse<TEnum>(value, true, out var enumValue) ? enumValue : null;
    }
}

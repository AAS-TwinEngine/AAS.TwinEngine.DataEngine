using System.Text.Json;
using System.Text.Json.Nodes;

namespace AAS.TwinEngine.ExportService.Infrastructure.Http.Clients;

/// <summary>
/// Removes null-valued properties from a JSON payload.
/// IDTA registries reject explicit nulls for optional fields, so they must be omitted before writing to a target.
/// </summary>
public static class NullPropertyStripper
{
    /// <summary>
    /// Returns <paramref name="rawJson"/> with every null-valued object property removed.
    /// The input is returned unchanged when it is not valid JSON or contains no nulls.
    /// </summary>
    public static string Strip(string rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return rawJson;
        }

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(rawJson);
        }
        catch (JsonException)
        {
            return rawJson;
        }

        if (root is null)
        {
            return rawJson;
        }

        StripNode(root);
        return root.ToJsonString();
    }

    private static void StripNode(JsonNode node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var name in obj.Where(p => p.Value is null).Select(p => p.Key).ToList())
                {
                    _ = obj.Remove(name);
                }

                foreach (var child in obj.Select(p => p.Value).OfType<JsonNode>().ToList())
                {
                    StripNode(child);
                }

                break;

            case JsonArray array:
                foreach (var child in array.OfType<JsonNode>().ToList())
                {
                    StripNode(child);
                }

                break;
        }
    }
}

using System.Text.Json;
using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRegistry;
using AAS.TwinEngine.DataEngine.Infrastructure.Shared;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Caching;

public static class DescriptorSerializer
{
    public static string SerializeShellDescriptor(ShellDescriptor descriptor)
    {
        var node = new JsonObject();

        AddArray(node, JsonPropertyNames.Description, descriptor.Description, Jsonization.Serialize.ToJsonObject);
        AddArray(node, JsonPropertyNames.DisplayName, descriptor.DisplayName, Jsonization.Serialize.ToJsonObject);
        AddArray(node, JsonPropertyNames.Extensions, descriptor.Extensions, Jsonization.Serialize.ToJsonObject);

        AddNode(node, JsonPropertyNames.Administration, descriptor.Administration, Jsonization.Serialize.ToJsonObject);

        AddValue(node, JsonPropertyNames.AssetKind, descriptor.AssetKind?.ToString());
        AddValue(node, JsonPropertyNames.AssetType, descriptor.AssetType?.ToString());

        AddSerialized(node, JsonPropertyNames.Endpoints, descriptor.Endpoints);

        AddValue(node, JsonPropertyNames.GlobalAssetId, descriptor.GlobalAssetId);
        AddValue(node, JsonPropertyNames.IdShort, descriptor.IdShort);
        AddValue(node, JsonPropertyNames.Id, descriptor.Id);

        AddSpecificAssetIds(node, descriptor.SpecificAssetIds);
        AddSubmodelDescriptors(node, descriptor.SubmodelDescriptors);

        return node.ToJsonString();
    }

    public static ShellDescriptor DeserializeShellDescriptor(string json)
    {
        var node = JsonNode.Parse(json);

        return new ShellDescriptor
        {
            Description = AasJsonNodeDeserializer.DeserializeAasArray(node?[JsonPropertyNames.Description], Jsonization.Deserialize.LangStringTextTypeFrom),

            DisplayName = AasJsonNodeDeserializer.DeserializeAasArray(node?[JsonPropertyNames.DisplayName], Jsonization.Deserialize.LangStringNameTypeFrom),

            Extensions = AasJsonNodeDeserializer.DeserializeAasArray(node?[JsonPropertyNames.Extensions], Jsonization.Deserialize.ExtensionFrom),

            Administration = AasJsonNodeDeserializer.DeserializeAasNode(node?[JsonPropertyNames.Administration], Jsonization.Deserialize.AdministrativeInformationFrom),

            AssetKind = AasJsonNodeDeserializer.DeserializeEnum<AssetKind>(node?[JsonPropertyNames.AssetKind]),

            AssetType = AasJsonNodeDeserializer.DeserializeEnum<AssetKind>(node?[JsonPropertyNames.AssetType]),

            Endpoints = node?[JsonPropertyNames.Endpoints]?.Deserialize<List<EndpointData>>(),

            GlobalAssetId = node?[JsonPropertyNames.GlobalAssetId]?.GetValue<string>(),
            IdShort = node?[JsonPropertyNames.IdShort]?.GetValue<string>(),
            Id = node?[JsonPropertyNames.Id]?.GetValue<string>(),

            SpecificAssetIds = AasJsonNodeDeserializer.DeserializeAasArray(node?[JsonPropertyNames.SpecificAssetIds], Jsonization.Deserialize.SpecificAssetIdFrom),

            SubmodelDescriptors = DeserializeSubmodelDescriptors(node?[JsonPropertyNames.SubmodelDescriptors])
        };
    }

    public static string SerializeSubmodelDescriptor(SubmodelDescriptor descriptor)
    {
        var node = new JsonObject();

        AddArray(node, JsonPropertyNames.Description, descriptor.Description, Jsonization.Serialize.ToJsonObject);
        AddArray(node, JsonPropertyNames.DisplayName, descriptor.DisplayName, Jsonization.Serialize.ToJsonObject);
        AddArray(node, JsonPropertyNames.Extensions, descriptor.Extensions, Jsonization.Serialize.ToJsonObject);

        AddNode(node, JsonPropertyNames.Administration, descriptor.Administration, Jsonization.Serialize.ToJsonObject);

        AddValue(node, JsonPropertyNames.IdShort, descriptor.IdShort);
        AddValue(node, JsonPropertyNames.Id, descriptor.Id);

        AddNode(node, JsonPropertyNames.SemanticId, descriptor.SemanticId, Jsonization.Serialize.ToJsonObject);

        AddArray(node, JsonPropertyNames.SupplementalSemanticId,descriptor.SupplementalSemanticId, Jsonization.Serialize.ToJsonObject);

        AddSerialized(node, JsonPropertyNames.Endpoints, descriptor.Endpoints);

        return node.ToJsonString();
    }

    public static SubmodelDescriptor DeserializeSubmodelDescriptor(string json)
    {
        var node = JsonNode.Parse(json);

        return new SubmodelDescriptor
        {
            Description = AasJsonNodeDeserializer.DeserializeAasArray(node?[JsonPropertyNames.Description], Jsonization.Deserialize.LangStringTextTypeFrom),

            DisplayName = AasJsonNodeDeserializer.DeserializeAasArray(node?[JsonPropertyNames.DisplayName], Jsonization.Deserialize.LangStringNameTypeFrom),

            Extensions = AasJsonNodeDeserializer.DeserializeAasArray(node?[JsonPropertyNames.Extensions], Jsonization.Deserialize.ExtensionFrom),

            Administration = AasJsonNodeDeserializer.DeserializeAasNode(node?[JsonPropertyNames.Administration], Jsonization.Deserialize.AdministrativeInformationFrom),

            IdShort = node?[JsonPropertyNames.IdShort]?.GetValue<string>(),
            Id = node?[JsonPropertyNames.Id]?.GetValue<string>(),

            SemanticId = AasJsonNodeDeserializer.DeserializeAasNode(node?[JsonPropertyNames.SemanticId], Jsonization.Deserialize.ReferenceFrom),

            SupplementalSemanticId = AasJsonNodeDeserializer.DeserializeAasArray(node?[JsonPropertyNames.SupplementalSemanticId], Jsonization.Deserialize.ReferenceFrom),

            Endpoints = node?[JsonPropertyNames.Endpoints]?.Deserialize<List<EndpointData>>()
        };
    }

    private static void AddValue(JsonObject node, string name, string? value)
    {
        if (value is not null)
        {
            node[name] = value;
        }
    }

    private static void AddSerialized<T>(JsonObject node, string name, T? value)
    {
        if (value is not null)
        {
            node[name] = JsonSerializer.SerializeToNode(value);
        }
    }

    private static void AddNode<T>(JsonObject node, string name, T? value, Func<T, JsonObject> serializer) where T : class
    {
        if (value is not null)
        {
            node[name] = serializer(value);
        }
    }

    private static void AddArray<T>(JsonObject node, string name, IEnumerable<T>? values, Func<T, JsonObject> serializer)
    {
        if (values is null)
        {
            return;
        }

        node[name] = new JsonArray(
            values.Select(x => (JsonNode)serializer(x)).ToArray());
    }

    private static void AddSpecificAssetIds(JsonObject node, IEnumerable<ISpecificAssetId>? assetIds)
    {
        if (assetIds is null)
        {
            return;
        }

        var array = new JsonArray();

        foreach (var assetId in assetIds)
        {
            var item = Jsonization.Serialize.ToJsonObject(assetId);

            if (item[JsonPropertyNames.SemanticId] is JsonNode semanticId)
            {
                _ = item.Remove(JsonPropertyNames.SemanticId);
                item[JsonPropertyNames.ExternalSubjectId] = semanticId.DeepClone();
            }

            array.Add(item);
        }

        node[JsonPropertyNames.SpecificAssetIds] = array;
    }

    private static void AddSubmodelDescriptors(JsonObject node, IEnumerable<SubmodelDescriptor>? descriptors)
    {
        if (descriptors is null)
        {
            return;
        }

        node[JsonPropertyNames.SubmodelDescriptors] = new JsonArray(
            descriptors
                .Select(x => JsonNode.Parse(SerializeSubmodelDescriptor(x))!)
                .ToArray());
    }

    private static List<SubmodelDescriptor>? DeserializeSubmodelDescriptors(JsonNode? node)
    {
        if (node is not JsonArray array)
        {
            return null;
        }

        return [.. array.Select(x => DeserializeSubmodelDescriptor(x!.ToJsonString()))];
    }
}

internal static class JsonPropertyNames
{
    public const string Description = "description";
    public const string DisplayName = "displayName";
    public const string Extensions = "extensions";
    public const string Administration = "administration";
    public const string AssetKind = "assetKind";
    public const string AssetType = "assetType";
    public const string Endpoints = "endpoints";
    public const string GlobalAssetId = "globalAssetId";
    public const string IdShort = "idShort";
    public const string Id = "id";
    public const string SpecificAssetIds = "specificAssetIds";
    public const string SemanticId = "semanticId";
    public const string ExternalSubjectId = "externalSubjectId";
    public const string SubmodelDescriptors = "submodelDescriptors";
    public const string SupplementalSemanticId = "supplementalSemanticId";
}

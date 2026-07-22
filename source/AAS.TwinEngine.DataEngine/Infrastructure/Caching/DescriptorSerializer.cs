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
        if (descriptor.Description is not null)
        {
            node["description"] = new JsonArray(descriptor.Description.Select(Jsonization.Serialize.ToJsonObject).ToArray<JsonNode>());
        }
        if (descriptor.DisplayName is not null)
        {
            node["displayName"] = new JsonArray(descriptor.DisplayName.Select(Jsonization.Serialize.ToJsonObject).ToArray<JsonNode>());
        }
        if (descriptor.Extensions is not null)
        {
            node["extensions"] = new JsonArray(descriptor.Extensions.Select(Jsonization.Serialize.ToJsonObject).ToArray<JsonNode>());
        }
        if (descriptor.Administration is not null)
        {
            node["administration"] = Jsonization.Serialize.ToJsonObject(descriptor.Administration);
        }
        if (descriptor.AssetKind is not null)
        {
            node["assetKind"] = descriptor.AssetKind.ToString();
        }
        if (descriptor.AssetType is not null)
        {
            node["assetType"] = descriptor.AssetType.ToString();
        }
        if (descriptor.Endpoints is not null)
        {
            node["endpoints"] = JsonSerializer.SerializeToNode(descriptor.Endpoints);
        }
        if (descriptor.GlobalAssetId is not null)
        {
            node["globalAssetId"] = descriptor.GlobalAssetId;
        }
        if (descriptor.IdShort is not null)
        {
            node["idShort"] = descriptor.IdShort;
        }
        if (descriptor.Id is not null)
        {
            node["id"] = descriptor.Id;
        }
        if (descriptor.SpecificAssetIds is not null)
        {
            var array = new JsonArray();
            foreach (var specificAssetId in descriptor.SpecificAssetIds)
            {
                var itemNode = Jsonization.Serialize.ToJsonObject(specificAssetId);
                if (itemNode["semanticId"] is JsonNode semanticIdNode)
                {
                    _ = itemNode.Remove("semanticId");
                    itemNode["externalSubjectId"] = semanticIdNode.DeepClone();
                }
                array.Add(itemNode);
            }
            node["specificAssetIds"] = array;
        }
        if (descriptor.SubmodelDescriptors is not null)
        {
            node["submodelDescriptors"] = new JsonArray(descriptor.SubmodelDescriptors.Select(x => JsonNode.Parse(SerializeSubmodelDescriptor(x))!).ToArray<JsonNode>());
        }
        return node.ToJsonString();
    }

    public static ShellDescriptor DeserializeShellDescriptor(string json)
    {
        var node = JsonNode.Parse(json);
        return new ShellDescriptor
        {
            Description = AasJsonNodeDeserializer.DeserializeAasArray(node?["description"], Jsonization.Deserialize.LangStringTextTypeFrom),
            DisplayName = AasJsonNodeDeserializer.DeserializeAasArray(node?["displayName"], Jsonization.Deserialize.LangStringNameTypeFrom),
            Extensions = AasJsonNodeDeserializer.DeserializeAasArray(node?["extensions"], Jsonization.Deserialize.ExtensionFrom),
            Administration = AasJsonNodeDeserializer.DeserializeAasNode(node?["administration"], Jsonization.Deserialize.AdministrativeInformationFrom),
            AssetKind = AasJsonNodeDeserializer.DeserializeEnum<AssetKind>(node?["assetKind"]),
            AssetType = AasJsonNodeDeserializer.DeserializeEnum<AssetKind>(node?["assetType"]),
            Endpoints = node?["endpoints"]?.Deserialize<List<EndpointData>>(),
            GlobalAssetId = node?["globalAssetId"]?.GetValue<string>(),
            IdShort = node?["idShort"]?.GetValue<string>(),
            Id = node?["id"]?.GetValue<string>(),
            SpecificAssetIds = AasJsonNodeDeserializer.DeserializeAasArray(node?["specificAssetIds"], Jsonization.Deserialize.SpecificAssetIdFrom),
            SubmodelDescriptors = node?["submodelDescriptors"] is JsonArray submodelArray
                ? submodelArray.Select(x => DeserializeSubmodelDescriptor(x!.ToJsonString())).ToList()
                : null
        };
    }

    public static string SerializeSubmodelDescriptor(SubmodelDescriptor descriptor)
    {
        var node = new JsonObject();
        if (descriptor.Description is not null)
        {
            node["description"] = new JsonArray(descriptor.Description.Select(Jsonization.Serialize.ToJsonObject).ToArray<JsonNode>());
        }
        if (descriptor.DisplayName is not null)
        {
            node["displayName"] = new JsonArray(descriptor.DisplayName.Select(Jsonization.Serialize.ToJsonObject).ToArray<JsonNode>());
        }
        if (descriptor.Extensions is not null)
        {
            node["extensions"] = new JsonArray(descriptor.Extensions.Select(Jsonization.Serialize.ToJsonObject).ToArray<JsonNode>());
        }
        if (descriptor.Administration is not null)
        {
            node["administration"] = Jsonization.Serialize.ToJsonObject(descriptor.Administration);
        }
        if (descriptor.IdShort is not null)
        {
            node["idShort"] = descriptor.IdShort;
        }
        if (descriptor.Id is not null)
        {
            node["id"] = descriptor.Id;
        }
        if (descriptor.SemanticId is not null)
        {
            node["semanticId"] = Jsonization.Serialize.ToJsonObject(descriptor.SemanticId);
        }
        if (descriptor.SupplementalSemanticId is not null)
        {
            node["supplementalSemanticId"] = new JsonArray(descriptor.SupplementalSemanticId.Select(x => Jsonization.Serialize.ToJsonObject(x)).ToArray<JsonNode>());
        }
        if (descriptor.Endpoints is not null)
        {
            node["endpoints"] = JsonSerializer.SerializeToNode(descriptor.Endpoints);
        }
        return node.ToJsonString();
    }

    public static SubmodelDescriptor DeserializeSubmodelDescriptor(string json)
    {
        var node = JsonNode.Parse(json);
        return new SubmodelDescriptor
        {
            Description = AasJsonNodeDeserializer.DeserializeAasArray(node?["description"], Jsonization.Deserialize.LangStringTextTypeFrom),
            DisplayName = AasJsonNodeDeserializer.DeserializeAasArray(node?["displayName"], Jsonization.Deserialize.LangStringNameTypeFrom),
            Extensions = AasJsonNodeDeserializer.DeserializeAasArray(node?["extensions"], Jsonization.Deserialize.ExtensionFrom),
            Administration = AasJsonNodeDeserializer.DeserializeAasNode(node?["administration"], Jsonization.Deserialize.AdministrativeInformationFrom),
            IdShort = node?["idShort"]?.GetValue<string>(),
            Id = node?["id"]?.GetValue<string>(),
            SemanticId = AasJsonNodeDeserializer.DeserializeAasNode(node?["semanticId"], Jsonization.Deserialize.ReferenceFrom),
            SupplementalSemanticId = AasJsonNodeDeserializer.DeserializeAasArray(node?["supplementalSemanticId"], Jsonization.Deserialize.ReferenceFrom),
            Endpoints = node?["endpoints"]?.Deserialize<List<EndpointData>>()
        };
    }
}

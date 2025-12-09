using System.Text.Json.Serialization;

using AAS.TwinEngine.DataEngine.DomainModel.Shared;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRegistry;

using AasCore.Aas3_0;

namespace AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;

public class ShellDescriptor
{
    [JsonPropertyName("description")]
    public IList<LangStringTextType>? Description { get; init; }

    [JsonPropertyName("displayName")]
    public IList<LangStringNameType>? DisplayName { get; init; }

    [JsonPropertyName("extensions")]
    public IList<Extension>? Extensions { get; init; }

    [JsonPropertyName("administration")]
    public AdministrativeInformation? Administration { get; set; }

    [JsonPropertyName("assetKind")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AssetKind? AssetKind { get; set; }

    [JsonPropertyName("assetType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AssetKind? AssetType { get; set; }

    [JsonPropertyName("endpoints")]
    public IList<EndpointData>? Endpoints { get; set; }

    [JsonPropertyName("globalAssetId")]
    public string? GlobalAssetId { get; set; }

    [JsonPropertyName("idShort")]
    public string? IdShort { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("specificAssetIds")]
    public IList<SpecificAssetId>? SpecificAssetIds { get; set; }

    [JsonPropertyName("submodelDescriptors")]
    public IList<SubmodelDescriptor>? SubmodelDescriptors { get; init; }

    public static ShellDescriptor CreateDefault()
    {
        return new ShellDescriptor
        {
            AssetKind = AasCore.Aas3_0.AssetKind.Type,
            AssetType = AasCore.Aas3_0.AssetKind.Type,
            GlobalAssetId = string.Empty,
            IdShort = string.Empty,
            Id = string.Empty,
            Endpoints = new List<EndpointData>
            {
                new()
                {
                    Interface = "AAS-3.0",
                    ProtocolInformation = new ProtocolInformationData
                    {
                        Href = string.Empty,
                        EndpointProtocol = "http"
                    }
                }
            }
        };
    }
}

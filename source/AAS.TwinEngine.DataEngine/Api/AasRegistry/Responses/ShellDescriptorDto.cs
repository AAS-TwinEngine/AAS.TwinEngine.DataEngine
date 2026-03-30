using System.Text.Json.Serialization;

using AAS.TwinEngine.DataEngine.Api.Shared;
using AAS.TwinEngine.DataEngine.Api.SubmodelRegistry.Responses;

using AasCore.Aas3_0;

namespace AAS.TwinEngine.DataEngine.Api.AasRegistry.Responses;

public class ShellDescriptorDto
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
    public AssetKind? AssetKind { get; set; }

    [JsonPropertyName("assetType")]
    public AssetKind? AssetType { get; set; }

    [JsonPropertyName("endpoints")]
    public IList<EndpointDto>? Endpoints { get; init; }

    [JsonPropertyName("globalAssetId")]
    public string? GlobalAssetId { get; set; }

    [JsonPropertyName("idShort")]
    public string? IdShort { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("specificAssetIds")]
    public IList<SpecificAssetId>? SpecificAssetIds { get; init; }

    [JsonPropertyName("submodelDescriptors")]
    public IList<SubmodelDescriptorDto>? SubmodelDescriptors { get; init; }
}

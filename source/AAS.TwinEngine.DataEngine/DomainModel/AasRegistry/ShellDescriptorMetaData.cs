using System.Text.Json.Serialization;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;

public class ShellDescriptorMetaData
{
    [JsonPropertyName("globalAssetId")]
    public string? GlobalAssetId { get; set; }

    [JsonPropertyName("idShort")]
    public string? IdShort { get; set; }

    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("assetKind")]
    public string? AssetKind { get; set; }

    [JsonPropertyName("assetType")]
    public string? AssetType { get; set; }

    [JsonIgnore]
    public AasCore.Aas3_1.AssetKind? ParsedAssetKind
        => Enum.TryParse<AasCore.Aas3_1.AssetKind>(AssetKind, true, out var parsed) ? parsed : null;

    [JsonPropertyName("specificAssetIds")]
    public IList<SpecificAssetId>? SpecificAssetIds { get; init; }

    [JsonPropertyName("href")]
    public string? Href { get; set; }
}

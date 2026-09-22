using System.Text.Json.Serialization;

namespace AAS.TwinEngine.DataEngine.DomainModel.AasRepository;

public class AssetData
{
    [JsonPropertyName("globalAssetId")]
    public string? GlobalAssetId { get; set; }

    [JsonPropertyName("assetKind")]
    public string? AssetKind { get; set; }

    [JsonPropertyName("assetType")]
    public string? AssetType { get; set; }

    [JsonIgnore]
    public AasCore.Aas3_1.AssetKind? ParsedAssetKind
        => Enum.TryParse<AasCore.Aas3_1.AssetKind>(AssetKind, true, out var parsed) ? parsed : null;

    [JsonPropertyName("specificAssetIds")]
    public IList<SpecificAssetIdsData>? SpecificAssetIds { get; init; } = [];

    [JsonPropertyName("defaultThumbnail")]
    public DefaultThumbnailData? DefaultThumbnail { get; set; }
}

public class DefaultThumbnailData
{
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("contentType")]
    public string? ContentType { get; set; }
}

public class SpecificAssetIdsData
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }
}

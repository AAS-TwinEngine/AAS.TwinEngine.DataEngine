using System.Text.Json.Serialization;

namespace Aas.TwinEngine.Plugin.TestPlugin.Infrastructure.DataAccess.Entity;

public class MetaDataEntity
{
    [JsonPropertyName("globalAssetId")]
    public string GlobalAssetId { get; set; } = string.Empty;

    [JsonPropertyName("idShort")]
    public string IdShort { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("specificAssetIds")]
    public List<SpecificAssetIdEntity>? SpecificAssetIds { get; set; }

    [JsonPropertyName("assetInformationData")]
    public AssetInformationDataEntity? AssetInformationData { get; set; }
}

public class SpecificAssetIdEntity
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

public class AssetInformationDataEntity
{
    [JsonPropertyName("globalAssetId")]
    public string? GlobalAssetId { get; set; }

    [JsonPropertyName("defaultThumbnail")]
    public DefaultThumbnailDataEntity? DefaultThumbnail { get; set; }
}

public class DefaultThumbnailDataEntity
{
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("contentType")]
    public string? ContentType { get; set; }
}

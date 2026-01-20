using System.Text.Json.Serialization;

namespace Aas.TwinEngine.Plugin.TestPlugin.Api.MetaData.Responses;

public class AssetDto
{
    [JsonPropertyName("globalAssetId")]
    public string? GlobalAssetId { get; set; }

    [JsonPropertyName("specificAssetIds")]
    public List<SpecificAssetIdsDto>? SpecificAssetIds { get; set; }

    [JsonPropertyName("defaultThumbnail")]
    public DefaultThumbnailDto? DefaultThumbnail { get; set; }
}

public class DefaultThumbnailDto
{
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("contentType")]
    public string? ContentType { get; set; }
}

using System.Text.Json.Serialization;

using AasCore.Aas3_0;

namespace AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;

public class ShellDescriptorMetaData
{
    [JsonPropertyName("globalAssetId")]
    public string? GlobalAssetId { get; set; }

    [JsonPropertyName("idShort")]
    public string? IdShort { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("specificAssetIds")]
    public IList<SpecificAssetId>? SpecificAssetIds { get; init; }

    [JsonPropertyName("href")]
    public string? Href { get; set; }
}

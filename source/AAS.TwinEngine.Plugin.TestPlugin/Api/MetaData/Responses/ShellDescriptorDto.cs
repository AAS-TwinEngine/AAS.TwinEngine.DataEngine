using System.Text.Json.Serialization;

namespace AAS.TwinEngine.Plugin.TestPlugin.Api.MetaData.Responses;

public class ShellDescriptorDto
{
    [JsonPropertyName("globalAssetId")]
    public string? GlobalAssetId { get; set; }

    [JsonPropertyName("idShort")]
    public string? IdShort { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("specificAssetIds")]
    public List<SpecificAssetIdsDto>? SpecificAssetIds { get; set; }
}

public class SpecificAssetIdsDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }
}


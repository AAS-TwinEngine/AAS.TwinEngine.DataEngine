using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AAS.TwinEngine.DataEngine.Api.Discovery.Requests;

public class AssetLinkDto
{
    [JsonPropertyName("name")]
    [Required]
    public required string Name { get; set; }

    [JsonPropertyName("value")]
    [Required]
    public required string Value { get; set; }
}

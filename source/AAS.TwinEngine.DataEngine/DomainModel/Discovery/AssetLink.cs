using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AAS.TwinEngine.DataEngine.DomainModel.Discovery;

public class AssetLink
{
    [JsonPropertyName("name")]
    [Required]
    public required string Name { get; set; }

    [JsonPropertyName("value")]
    [Required]
    public required string Value { get; set; }
}

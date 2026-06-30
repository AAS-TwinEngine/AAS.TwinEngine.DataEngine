using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace AAS.TwinEngine.DataEngine.Api.Discovery.Requests;

/// <summary>
/// IDTA discovery asset link used to search for matching AAS identifiers.
/// </summary>
public class AssetLinkDto
{
    /// <summary>
    /// Asset link type or key.
    /// </summary>
    /// <example>globalAssetId</example>
    [JsonPropertyName("name")]
    [Required]
    [Description("Asset link key. Example: globalAssetId.")]
    [DefaultValue("globalAssetId")]
    public required string Name { get; set; }

    /// <summary>
    /// Asset link value.
    /// </summary>
    /// <example>https://example.com/ids/asset/4711</example>
    [JsonPropertyName("value")]
    [Required]
    [Description("Asset link value. Example: https://example.com/ids/asset/4711.")]
    [DefaultValue("https://example.com/ids/asset/4711")]
    public required string Value { get; set; }
}

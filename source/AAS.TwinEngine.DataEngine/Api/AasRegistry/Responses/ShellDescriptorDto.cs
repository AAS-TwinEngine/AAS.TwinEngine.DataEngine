using System.Text.Json.Serialization;
using System.ComponentModel;

using AAS.TwinEngine.DataEngine.Api.Shared;
using AAS.TwinEngine.DataEngine.Api.SubmodelRegistry.Responses;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.Api.AasRegistry.Responses;

/// <summary>
/// Descriptor metadata for one Asset Administration Shell.
/// </summary>
public class ShellDescriptorDto
{
    /// <summary>
    /// Human-readable description texts.
    /// </summary>
    [JsonPropertyName("description")]
    [Description("Localized description entries for the shell descriptor.")]
    public IList<LangStringTextType>? Description { get; init; }

    /// <summary>
    /// Human-readable display names.
    /// </summary>
    [JsonPropertyName("displayName")]
    [Description("Localized display name entries for the shell descriptor.")]
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
    [Description("Global asset identifier associated with the shell.")]
    [DefaultValue("https://example.com/ids/asset/4711")]
    public string? GlobalAssetId { get; set; }

    [JsonPropertyName("idShort")]
    public string? IdShort { get; set; }

    [JsonPropertyName("id")]
    [Description("Global shell identifier.")]
    [DefaultValue("https://example.com/ids/aas/1170_1160_3052_6568")]
    public string? Id { get; set; }

    [JsonPropertyName("specificAssetIds")]
    public IList<SpecificAssetId>? SpecificAssetIds { get; init; }

    [JsonPropertyName("submodelDescriptors")]
    public IList<SubmodelDescriptorDto>? SubmodelDescriptors { get; init; }
}

using System.Text.Json.Serialization;
using System.ComponentModel;

using AAS.TwinEngine.DataEngine.Api.Shared;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.Api.SubmodelRegistry.Responses;

/// <summary>
/// Descriptor metadata for one submodel.
/// </summary>
public class SubmodelDescriptorDto
{
    /// <summary>
    /// Human-readable description texts.
    /// </summary>
    [JsonPropertyName("description")]
    [Description("Localized description entries for the submodel descriptor.")]
    public IList<LangStringTextType>? Description { get; init; }

    [JsonPropertyName("displayName")]
    public IList<LangStringNameType>? DisplayName { get; init; }

    [JsonPropertyName("extensions")]
    public IList<Extension>? Extensions { get; init; }

    [JsonPropertyName("administration")]
    public AdministrativeInformation? Administration { get; set; }

    [JsonPropertyName("idShort")]
    public string? IdShort { get; set; }

    [JsonPropertyName("id")]
    [Description("Global submodel identifier.")]
    [DefaultValue("https://example.com/ids/submodel/1234")]
    public string? Id { get; set; }

    [JsonPropertyName("semanticId")]
    public Reference? SemanticId { get; set; }

    [JsonPropertyName("supplementalSemanticId")]
    public IList<Reference>? SupplementalSemanticId { get; init; }

    [JsonPropertyName("endpoints")]
    public IList<EndpointDto>? Endpoints { get; init; }
}

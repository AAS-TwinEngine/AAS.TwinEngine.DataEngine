using System.Text.Json.Serialization;

using AAS.TwinEngine.DataEngine.DomainModel.Shared;

using AasCore.Aas3_0;

namespace AAS.TwinEngine.DataEngine.DomainModel.SubmodelRegistry;

public class SubmodelDescriptor
{
    [JsonPropertyName("description")]
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
    public string? Id { get; set; }

    [JsonPropertyName("semanticId")]
    public Reference? SemanticId { get; set; }

    [JsonPropertyName("supplementalSemanticId")]
    public IList<Reference>? SupplementalSemanticId { get; init; }

    [JsonPropertyName("endpoints")]
    public IList<EndpointData>? Endpoints { get; set; }
}

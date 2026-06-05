using System.Text.Json.Serialization;

using AasCore.Aas3_0;

namespace AAS.TwinEngine.DataEngine.DomainModel.Discovery;

public class SpecificAssetIdFilter
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("value")]
    public required string Value { get; set; }

    [JsonPropertyName("externalSubjectId")]
    public IReference? ExternalSubjectId { get; set; }

    [JsonPropertyName("semanticId")]
    public IReference? SemanticId { get; set; }
}

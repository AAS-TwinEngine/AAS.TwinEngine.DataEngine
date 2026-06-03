using System.Text.Json.Serialization;

namespace AAS.TwinEngine.DataEngine.DomainModel.Discovery;

public class SpecificAssetIdFilter
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("value")]
    public required string Value { get; set; }

    [JsonPropertyName("externalSubjectId")]
    public ReferenceFilter? ExternalSubjectId { get; set; }

    [JsonPropertyName("semanticId")]
    public ReferenceFilter? SemanticId { get; set; }
}

public class ReferenceFilter
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("keys")]
    public required IList<KeyFilter> Keys { get; set; }
}

public class KeyFilter
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("value")]
    public required string Value { get; set; }
}

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace AAS.TwinEngine.DataEngine.Api.Shared;

/// <summary>
/// Paged response for content modifier routes. Depending on the modifier, <see cref="Result"/> is a JSON array
/// (Metadata, Reference, Path) or a JSON object (Value of submodel elements).
/// </summary>
public class PagedContentDto
{
    [JsonPropertyName("paging_metadata")]
    public PagingMetaDataDto? PagingMetaData { get; set; }

    [JsonPropertyName("result")]
    public JsonNode Result { get; init; } = new JsonArray();
}

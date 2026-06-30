using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.ComponentModel;

using AAS.TwinEngine.DataEngine.Api.Shared;

namespace AAS.TwinEngine.DataEngine.Api.AasRepository.Responses;

/// <summary>
/// Paginated response containing Asset Administration Shell documents.
/// </summary>
public class ShellsDto
{
    /// <summary>
    /// Cursor metadata for paginated result retrieval.
    /// </summary>
    [JsonPropertyName("paging_metadata")]
    [Description("Pagination metadata for shell retrieval.")]
    public PagingMetaDataDto? PagingMetaData { get; set; }

    /// <summary>
    /// Shell documents in JSON form.
    /// </summary>
    [JsonPropertyName("result")]
    [Description("List of AAS JSON documents.")]
    public IList<JsonObject>? Result { get; init; }
}

using System.Text.Json.Serialization;
using System.ComponentModel;

using AAS.TwinEngine.DataEngine.Api.Shared;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.Api.AasRepository.Responses;

/// <summary>
/// Paginated response containing submodel references.
/// </summary>
public class SubmodelRefDto
{
    /// <summary>
    /// Cursor metadata for paginated result retrieval.
    /// </summary>
    [JsonPropertyName("paging_metadata")]
    [Description("Pagination metadata for submodel reference retrieval.")]
    public PagingMetaDataDto? PagingMetaData { get; set; }

    /// <summary>
    /// Submodel references linked to the selected shell.
    /// </summary>
    [JsonPropertyName("result")]
    [Description("List of submodel references.")]
    public IList<IReference>? Result { get; init; }
}

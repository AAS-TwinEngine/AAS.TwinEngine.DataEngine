using System.Text.Json.Serialization;
using System.ComponentModel;

using AAS.TwinEngine.DataEngine.Api.Shared;

namespace AAS.TwinEngine.DataEngine.Api.AasRegistry.Responses;

/// <summary>
/// Paginated response containing shell descriptors.
/// </summary>
public class ShellDescriptorsDto
{
    /// <summary>
    /// Cursor metadata for paginated result retrieval.
    /// </summary>
    [JsonPropertyName("paging_metadata")]
    [Description("Pagination metadata for shell descriptor retrieval.")]
    public PagingMetaDataDto? PagingMetaData { get; set; }

    /// <summary>
    /// Shell descriptor list.
    /// </summary>
    [JsonPropertyName("result")]
    [Description("List of shell descriptors.")]
    public IList<ShellDescriptorDto>? Result { get; init; }
}


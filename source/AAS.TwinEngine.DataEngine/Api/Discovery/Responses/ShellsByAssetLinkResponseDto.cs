using System.Text.Json.Serialization;
using System.ComponentModel;

using AAS.TwinEngine.DataEngine.Api.Shared;

namespace AAS.TwinEngine.DataEngine.Api.Discovery.Responses;

/// <summary>
/// Discovery result containing matching AAS identifiers.
/// </summary>
public class ShellsByAssetLinkResponseDto
{
    /// <summary>
    /// Cursor metadata for paginated result retrieval.
    /// </summary>
    [JsonPropertyName("paging_metadata")]
    [Description("Pagination metadata for continued discovery queries.")]
    public PagingMetaDataDto? PagingMetaData { get; set; }

    /// <summary>
    /// Matching AAS identifiers.
    /// </summary>
    /// <example>["https://example.com/ids/aas/1170_1160_3052_6568"]</example>
    [JsonPropertyName("result")]
    [Description("AAS identifiers matching the submitted asset links.")]
    public IList<string>? Result { get; set; }
}

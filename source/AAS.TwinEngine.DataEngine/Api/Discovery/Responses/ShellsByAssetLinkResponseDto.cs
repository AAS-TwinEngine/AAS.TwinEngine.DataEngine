using System.Text.Json.Serialization;

using AAS.TwinEngine.DataEngine.Api.Shared;

namespace AAS.TwinEngine.DataEngine.Api.Discovery.Responses;

public class ShellsByAssetLinkResponseDto
{
    [JsonPropertyName("paging_metadata")]
    public PagingMetaDataDto? PagingMetaData { get; set; }

    [JsonPropertyName("result")]
    public IList<string>? Result { get; set; }
}

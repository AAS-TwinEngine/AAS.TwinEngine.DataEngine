using System.Text.Json.Serialization;

using AAS.TwinEngine.DataEngine.Api.Shared;

namespace AAS.TwinEngine.DataEngine.Api.AasRegistry.Responses;

public class ShellDescriptorsDto
{
    [JsonPropertyName("paging_metadata")]
    public PagingMetaDataDto? PagingMetaData { get; set; }

    [JsonPropertyName("result")]
    public IList<ShellDescriptorDto>? Result { get; init; }
}


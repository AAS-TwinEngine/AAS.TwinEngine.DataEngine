using System.Text.Json.Serialization;

using AAS.TwinEngine.DataEngine.Api.Shared;

namespace AAS.TwinEngine.DataEngine.Api.SubmodelRegistry.Responses;

public class SubmodelDescriptorsDto
{
    [JsonPropertyName("paging_metadata")]
    public PagingMetaDataDto? PagingMetaData { get; set; }

    [JsonPropertyName("result")]
    public IList<SubmodelDescriptorDto>? Result { get; init; }
}

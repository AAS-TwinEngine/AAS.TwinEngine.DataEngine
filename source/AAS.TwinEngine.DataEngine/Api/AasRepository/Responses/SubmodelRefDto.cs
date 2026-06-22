using System.Text.Json.Serialization;

using AAS.TwinEngine.DataEngine.Api.Shared;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.Api.AasRepository.Responses;

public class SubmodelRefDto
{
    [JsonPropertyName("paging_metadata")]
    public PagingMetaDataDto? PagingMetaData { get; set; }

    [JsonPropertyName("result")]
    public IList<IReference>? Result { get; init; }
}

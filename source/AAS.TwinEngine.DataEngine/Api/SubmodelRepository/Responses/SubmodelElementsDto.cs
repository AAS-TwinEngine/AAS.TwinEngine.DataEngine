using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using AAS.TwinEngine.DataEngine.Api.Shared;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Responses;

public class SubmodelElementsDto
{
    [JsonPropertyName("paging_metadata")]
    public PagingMetaDataDto? PagingMetaData { get; set; }

    [JsonPropertyName("result")]
    public IList<JsonObject> Result { get; init; } = [];
}

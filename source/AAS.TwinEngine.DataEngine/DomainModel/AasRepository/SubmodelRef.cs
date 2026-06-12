using System.Text.Json.Serialization;

using AAS.TwinEngine.DataEngine.DomainModel.Shared;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.DomainModel.AasRepository;

public class SubmodelRef
{
    [JsonPropertyName("paging_metadata")]
    public PagingMetaData? PagingMetaData { get; set; }

    [JsonPropertyName("result")]
    public IList<IReference>? Result { get; init; }
}

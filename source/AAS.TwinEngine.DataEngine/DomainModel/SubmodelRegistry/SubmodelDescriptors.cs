using System.Text.Json.Serialization;

using AAS.TwinEngine.DataEngine.DomainModel.Shared;

namespace AAS.TwinEngine.DataEngine.DomainModel.SubmodelRegistry;

public class SubmodelDescriptors
{
    [JsonPropertyName("paging_metadata")]
    public PagingMetaData? PagingMetaData { get; set; }

    [JsonPropertyName("result")]
    public IList<SubmodelDescriptor>? Result { get; init; }
}

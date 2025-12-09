using System.Text.Json.Serialization;

using AAS.TwinEngine.DataEngine.DomainModel.Shared;

namespace AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;

public class ShellDescriptors
{
    [JsonPropertyName("paging_metadata")]
    public PagingMetaData? PagingMetaData { get; set; }

    [JsonPropertyName("result")]
    public IList<ShellDescriptor>? Result { get; init; }
}

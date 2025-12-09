using System.Text.Json.Serialization;

using AAS.TwinEngine.DataEngine.DomainModel.Shared;

namespace AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;

public class ShellDescriptorsMetaData
{
    [JsonPropertyName("paging_metadata")]
    public PagingMetaData? PagingMetaData { get; set; }

    [JsonPropertyName("result")]
    public List<ShellDescriptorMetaData>? ShellDescriptors { get; set; } = new();
}

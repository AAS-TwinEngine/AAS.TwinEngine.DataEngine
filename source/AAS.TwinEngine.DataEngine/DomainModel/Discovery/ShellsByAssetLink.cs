using System.Text.Json.Serialization;

using AAS.TwinEngine.DataEngine.DomainModel.Shared;

namespace AAS.TwinEngine.DataEngine.DomainModel.Discovery;

public class ShellsByAssetLink
{
    [JsonPropertyName("paging_metadata")]
    public PagingMetaData? PagingMetaData { get; set; }

    [JsonPropertyName("result")]
    public IList<string>? Result { get; set; }
}

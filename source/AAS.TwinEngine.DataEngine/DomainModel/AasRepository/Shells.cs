using System.Text.Json.Serialization;

using AAS.TwinEngine.DataEngine.DomainModel.Shared;

using AasCore.Aas3_0;

namespace AAS.TwinEngine.DataEngine.DomainModel.AasRepository;

public class Shells
{
    [JsonPropertyName("paging_metadata")]
    public PagingMetaData? PagingMetaData { get; set; }

    [JsonPropertyName("result")]
    public IList<IAssetAdministrationShell>? Result { get; init; }
}

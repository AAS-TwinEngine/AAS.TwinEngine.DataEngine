using System.Text.Json.Serialization;

namespace AAS.TwinEngine.Plugin.TestPlugin.DomainModel.MetaData;

public class ShellDescriptorsData
{
    [JsonPropertyName("paging_metadata")]
    public PagingMetaData? PagingMetaData { get; set; }

    [JsonPropertyName("result")]
    public IList<ShellDescriptorData>? Result { get; init; }
}

public class PagingMetaData
{
    [JsonPropertyName("cursor")]
    public string? Cursor { get; set; }
}

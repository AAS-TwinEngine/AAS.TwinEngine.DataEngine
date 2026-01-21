using System.Text.Json.Serialization;

namespace AAS.TwinEngine.Plugin.TestPlugin.Api.MetaData.Responses;

public class ShellDescriptorsDto
{
    [JsonPropertyName("paging_metadata")]
    public PagingMetaDataDto? PagingMetaData { get; set; }

    [JsonPropertyName("result")]
    public IList<ShellDescriptorDto>? Result { get; init; }
}

public class PagingMetaDataDto
{
    [JsonPropertyName("cursor")]
    public string? Cursor { get; set; }
}

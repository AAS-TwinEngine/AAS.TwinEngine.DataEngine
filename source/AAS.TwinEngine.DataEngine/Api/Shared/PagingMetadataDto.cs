using System.Text.Json.Serialization;

namespace AAS.TwinEngine.DataEngine.Api.Shared;

public class PagingMetaDataDto
{
    [JsonPropertyName("cursor")]
    public string? Cursor { get; set; }
}

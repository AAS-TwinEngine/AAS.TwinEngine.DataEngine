using System.Text.Json.Serialization;

namespace AAS.TwinEngine.DataEngine.DomainModel.Shared;

public class PagingMetaData
{
    [JsonPropertyName("cursor")]
    public string? Cursor { get; set; }
}

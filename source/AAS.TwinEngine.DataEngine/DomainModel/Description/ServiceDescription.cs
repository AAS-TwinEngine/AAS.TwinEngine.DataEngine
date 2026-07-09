using System.Text.Json.Serialization;

namespace AAS.TwinEngine.DataEngine.DomainModel.Description;

public class ServiceDescription
{
    [JsonPropertyName("profiles")]
    public IList<string>? Profiles { get; init; }
}

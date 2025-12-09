using System.Text.Json;
using System.Text.Json.Serialization;

namespace AAS.TwinEngine.DataEngine.Api.Shared;

public static class JsonSerializationOptions
{
    public static readonly JsonSerializerOptions SerializeToElementWithEnum = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };
}

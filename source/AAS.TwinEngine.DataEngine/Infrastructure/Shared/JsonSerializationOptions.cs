using System.Text.Json;
using System.Text.Json.Serialization;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Shared;

public static class JsonSerializationOptions
{
    public static readonly JsonSerializerOptions FileAndHttpContent = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static readonly JsonSerializerOptions Serialization = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static readonly JsonSerializerOptions SerializationWithEnum = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static readonly JsonSerializerOptions DeserializationOption = new()
    {
        PropertyNameCaseInsensitive = true
    };
}

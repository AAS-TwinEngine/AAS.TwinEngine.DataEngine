using System.Text.Json;
using System.Text.Json.Serialization;

namespace AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Services.Validation;

internal static class JsonSchemaValidationSerialization
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
}

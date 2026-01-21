using Json.Schema;

namespace AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Services;

public interface IJsonSchemaValidator
{
    void ValidateResponseContent(string responseJson, JsonSchema requestSchema);
}

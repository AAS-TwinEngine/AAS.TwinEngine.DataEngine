using Json.Schema;

namespace Aas.TwinEngine.Plugin.TestPlugin.Api.Submodel.Services;

public interface IJsonSchemaValidator
{
    void ValidateResponseContent(string responseJson, JsonSchema requestSchema);
}

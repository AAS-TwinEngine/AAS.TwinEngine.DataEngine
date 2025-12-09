using Json.Schema;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Helper;

public interface IJsonSchemaValidator
{
    void ValidateResponseContent(string responseJson, JsonSchema requestSchema);
    void ValidateRequestSchema(JsonSchema schema);
}

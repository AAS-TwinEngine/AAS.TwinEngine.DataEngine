using System.Text.Json.Nodes;

namespace AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Services;

public interface IJsonSchemaSecurityValidator
{
    void ValidateSchemaComplexity(JsonNode rootNode);

    void ValidateSchemaContent(JsonNode rootNode);
}

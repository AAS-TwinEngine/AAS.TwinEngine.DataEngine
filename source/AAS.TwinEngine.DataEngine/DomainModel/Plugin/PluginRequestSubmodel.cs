namespace AAS.TwinEngine.DataEngine.DomainModel.Plugin;

public class PluginRequestSubmodel(string httpClientName, JsonContent jsonSchema)
{
    public string HttpClientName { get; init; } = httpClientName;
    public JsonContent JsonSchema { get; init; } = jsonSchema;
}

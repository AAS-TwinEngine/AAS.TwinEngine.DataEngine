namespace AAS.TwinEngine.DataEngine.DomainModel.Plugin;

public class PluginRequestSubmodelBatch(string httpClientName, JsonContent content)
{
    public string HttpClientName { get; init; } = httpClientName;
    public JsonContent Content { get; init; } = content;
}
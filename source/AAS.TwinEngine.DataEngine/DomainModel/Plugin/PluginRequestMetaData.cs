namespace AAS.TwinEngine.DataEngine.DomainModel.Plugin;

public class PluginRequestMetaData(string httpClientName, string aasIdentifier)
{
    public string HttpClientName { get; init; } = httpClientName;
    public string AasIdentifier { get; init; } = aasIdentifier;
}

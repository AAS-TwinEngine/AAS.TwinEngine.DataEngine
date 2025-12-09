using AAS.TwinEngine.DataEngine.DomainModel.Plugin;

using Json.Schema;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;

public interface IPluginRequestBuilder
{
    IList<PluginRequestSubmodel> Build(IDictionary<string, JsonSchema> jsonSchema);

    IList<PluginRequestMetaData> Build(IList<string> plugins, string? aasIdentifier = null);
}

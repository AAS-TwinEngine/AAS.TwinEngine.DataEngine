using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.DomainModel.Plugin;
using AAS.TwinEngine.DataEngine.Infrastructure.Monitoring;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Shared;

using Json.Schema;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Services;

public class PluginRequestBuilder(IPluginManifestHealthStatus pluginManifestHealthStatus) : IPluginRequestBuilder
{
    public IList<PluginRequestSubmodel> Build(IDictionary<string, JsonSchema> jsonSchema)
    {
        EnsureManifestIsHealthy();

        return jsonSchema
            .Select(kvp => new PluginRequestSubmodel(
                $"{PluginConfig.HttpClientNamePrefix}{kvp.Key}",
                CreateHttpContent(kvp.Value)))
            .ToList();
    }

    public IList<PluginRequestMetaData> Build(IList<string> plugins, string? aasIdentifier = null)
    {
        EnsureManifestIsHealthy();

        return plugins
            .Select(plugin => new PluginRequestMetaData(
                $"{PluginConfig.HttpClientNamePrefix}{plugin}",
                aasIdentifier ?? string.Empty))
            .ToList();
    }

    private void EnsureManifestIsHealthy()
    {
        if (!pluginManifestHealthStatus.IsHealthy)
        {
            throw new MultiPluginConflictException();
        }
    }

    private static JsonContent CreateHttpContent(JsonSchema jsonSchema) => JsonContent.Create(jsonSchema, options: JsonSerializationOptions.FileAndHttpContent);
}

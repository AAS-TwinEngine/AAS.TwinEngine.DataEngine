using AAS.TwinEngine.DataEngine.DomainModel.Plugin;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Extensions;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Helper;

public static class RegisterPluginHttpClients
{
    public static void RegisterHttpClients(
        IServiceCollection services,
        RetryConfig retryConfig,
        IReadOnlyCollection<PluginManifest> manifests)
    {
        foreach (var manifest in manifests)
        {
            _ = services.AddHttpClientWithResilience(
                $"{HttpClientNames.PluginDataProviderPrefix}{manifest.PluginName}",
                retryConfig,
                manifest.PluginUrl!
            );
        }
    }
}

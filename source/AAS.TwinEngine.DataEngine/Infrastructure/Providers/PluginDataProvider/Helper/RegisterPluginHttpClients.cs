using AAS.TwinEngine.DataEngine.DomainModel.Plugin;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Extensions;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Config;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Helper;

public static class RegisterPluginHttpClients
{
    public static void RegisterHttpClients(
        IServiceCollection services,
        IConfiguration configuration,
        IReadOnlyCollection<PluginManifest> manifests)
    {
        foreach (var manifest in manifests)
        {
            _ = services.AddHttpClientWithResilience(
                configuration,
                $"{PluginConfig.HttpClientNamePrefix}{manifest.PluginName}",
                HttpRetryPolicyOptions.PluginDataProvider,
                manifest.PluginUrl!
            );
        }
    }
}

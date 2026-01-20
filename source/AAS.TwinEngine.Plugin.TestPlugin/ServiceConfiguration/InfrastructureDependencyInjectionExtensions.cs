using Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.Manifest;
using Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.MetaData;
using Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.Submodel;
using Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.Submodel.Config;
using Aas.TwinEngine.Plugin.TestPlugin.Infrastructure.Providers;
using Aas.TwinEngine.Plugin.TestPlugin.Infrastructure.Providers.Config;
using Aas.TwinEngine.Plugin.TestPlugin.Infrastructure.Providers.ManifestProvider;
using Aas.TwinEngine.Plugin.TestPlugin.Infrastructure.Providers.MetaDataProvider;
using Aas.TwinEngine.Plugin.TestPlugin.Infrastructure.Providers.SubmodelProviders;

namespace Aas.TwinEngine.Plugin.TestPlugin.ServiceConfiguration;

public static class InfrastructureDependencyInjectionExtensions
{
    public static void ConfigureInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<Semantics>().Bind(configuration.GetSection(Semantics.Section)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<Capabilities>().Bind(configuration.GetSection(Capabilities.Section)).ValidateDataAnnotations().ValidateOnStart();
        services.AddScoped<MockDataInitializer>();
        services.AddSingleton<ISubmodelProvider, SubmodelProvider>();
        services.AddSingleton<IMetaDataProvider, MetaDataProvider>();
        services.AddScoped<IManifestProvider, ManifestProvider>();
    }
}

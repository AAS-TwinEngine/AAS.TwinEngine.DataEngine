using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.Manifest;
using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.MetaData;
using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.Submodel;
using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.Submodel.Config;
using AAS.TwinEngine.Plugin.TestPlugin.Infrastructure.Providers;
using AAS.TwinEngine.Plugin.TestPlugin.Infrastructure.Providers.Config;
using AAS.TwinEngine.Plugin.TestPlugin.Infrastructure.Providers.ManifestProvider;
using AAS.TwinEngine.Plugin.TestPlugin.Infrastructure.Providers.MetaDataProvider;
using AAS.TwinEngine.Plugin.TestPlugin.Infrastructure.Providers.SubmodelProviders;

namespace AAS.TwinEngine.Plugin.TestPlugin.ServiceConfiguration;

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

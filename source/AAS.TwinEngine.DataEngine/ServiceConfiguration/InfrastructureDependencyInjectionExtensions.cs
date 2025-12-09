using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRegistry.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Config;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Helper;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRegistry.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Extensions;
using AAS.TwinEngine.DataEngine.Infrastructure.Monitoring;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.AasRegistryProvider.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.AasRegistryProvider.Services;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Helper;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Services;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.SubmodelRegistryProvider.Services;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.TemplateProvider.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.TemplateProvider.Services;

namespace AAS.TwinEngine.DataEngine.ServiceConfiguration;

public static class InfrastructureDependencyInjectionExtensions
{
    public static void ConfigureInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        _ = services.AddHttpClient();

        _ = services.AddScoped<PluginManifestInitializer>();
        _ = services.AddScoped<ITemplateProvider, TemplateProvider>();
        _ = services.AddScoped<ISubmodelTemplateMappingProvider, SubmodelTemplateMappingProvider>();
        _ = services.AddScoped<IShellTemplateMappingProvider, ShellTemplateMappingProvider>();
        _ = services.Configure<TemplateMappingRules>(configuration.GetSection(TemplateMappingRules.Section));
        _ = services.Configure<AasEnvironmentConfig>(configuration.GetSection(AasEnvironmentConfig.Section));
        _ = services.Configure<AasxExportOptions>(configuration.GetSection(AasxExportOptions.Section));
        _ = services.Configure<PluginConfig>(configuration.GetSection(PluginConfig.Section));

        var aasEnvironment = configuration.GetSection(AasEnvironmentConfig.Section).Get<AasEnvironmentConfig>();
        var plugins = configuration.GetSection(PluginConfig.Section).Get<PluginConfig>();

        _ = services.AddOptions<Semantics>().Bind(configuration.GetSection(Semantics.Section)).ValidateDataAnnotations().ValidateOnStart();
        _ = services.AddHttpClientWithResilience(configuration, AasEnvironmentConfig.AasEnvironmentRepoHttpClientName, HttpRetryPolicyOptions.TemplateProvider, aasEnvironment?.AasEnvironmentRepositoryBaseUrl!);
        _ = services.AddHttpClientWithResilience(configuration, AasEnvironmentConfig.AasRegistryHttpClientName, HttpRetryPolicyOptions.TemplateProvider, aasEnvironment?.AasRegistryBaseUrl!);
        _ = services.AddHttpClientWithResilience(configuration, AasEnvironmentConfig.SubmodelRegistryHttpClientName, HttpRetryPolicyOptions.SubmodelDescriptorProvider, aasEnvironment?.SubModelRegistryBaseUrl!);

        foreach (var plugin in plugins.Plugins)
        {
            _ = services.AddHttpClientWithResilience(configuration, PluginConfig.HttpClientNamePrefix + plugin.PluginName, HttpRetryPolicyOptions.PluginDataProvider, plugin?.PluginUrl);
        }

        _ = services.AddScoped<IPluginRequestBuilder, PluginRequestBuilder>();
        _ = services.AddScoped<IAasRegistryProvider, AasRegistryProvider>();
        _ = services.AddScoped<ICreateClient, HttpClientFactory>();
        _ = services.AddScoped<IPluginDataProvider, PluginDataProvider>();
        _ = services.AddScoped<IJsonSchemaValidator, JsonSchemaValidator>();
        _ = services.AddScoped<IPluginManifestProvider, PluginManifestProvider>();
        _ = services.AddScoped<IMultiPluginDataHandler, MultiPluginDataHandler>();
        _ = services.AddScoped<ISubmodelDescriptorProvider, SubmodelDescriptorProvider>();
        _ = services.Configure<HttpRetryPolicyOptions>(HttpRetryPolicyOptions.PluginDataProvider, configuration.GetSection($"{HttpRetryPolicyOptions.Section}:{HttpRetryPolicyOptions.PluginDataProvider}"));
        _ = services.Configure<HttpRetryPolicyOptions>(HttpRetryPolicyOptions.TemplateProvider, configuration.GetSection($"{HttpRetryPolicyOptions.Section}:{HttpRetryPolicyOptions.TemplateProvider}"));
        _ = services.Configure<HttpRetryPolicyOptions>(HttpRetryPolicyOptions.SubmodelDescriptorProvider, configuration.GetSection($"{HttpRetryPolicyOptions.Section}:{HttpRetryPolicyOptions.SubmodelDescriptorProvider}"));
        _ = services.Configure<HttpRetryPolicyOptions>(configuration.GetSection(HttpRetryPolicyOptions.Section));
        _ = services.Configure<AasRegistryPreComputed>(configuration.GetSection(AasRegistryPreComputed.Section));
        _ = services.Configure<MultiPluginConflictOptions>(configuration.GetSection(MultiPluginConflictOptions.Section));
        _ = services.AddSingleton<IPluginManifestHealthStatus, PluginManifestHealthStatus>();
        _ = services.AddHostedService<ShellDescriptorSyncHosted>();
    }
}

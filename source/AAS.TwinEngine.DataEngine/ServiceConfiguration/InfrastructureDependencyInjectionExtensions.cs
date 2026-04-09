using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRegistry.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Config;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Helper;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Shared;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRegistry.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.Config.Helper;
using AAS.TwinEngine.DataEngine.Infrastructure.Configuration.LegacyV1;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Headers;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Extensions;
using AAS.TwinEngine.DataEngine.Infrastructure.Monitoring;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.AasRegistryProvider.Services;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Helper;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Services;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.SubmodelRegistryProvider.Services;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.TemplateProvider.Services;
using AAS.TwinEngine.DataEngine.Infrastructure.Shared;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.ServiceConfiguration;

public static class InfrastructureDependencyInjectionExtensions
{
    public static void ConfigureInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        _ = services.AddHttpClient();

        _ = services.AddScoped<IRequestHeaderMapper, RequestHeaderMapper>();

        _ = services.AddScoped<IBaseUrlProvider, HttpRequestBaseUrlProvider>();

        _ = services.AddScoped<PluginManifestInitializer>();
        _ = services.AddScoped<ITemplateProvider, TemplateProvider>();
        _ = services.AddScoped<ISubmodelTemplateMappingProvider, SubmodelTemplateMappingProvider>();
        _ = services.AddScoped<IShellTemplateMappingProvider, ShellTemplateMappingProvider>();

        // ── V1 → V2 legacy adapters (IConfigureOptions<T>), no-op when V2 config is present ──
#pragma warning disable CS0618 // Obsolete — intentional V1 backward-compat registration
        _ = services.AddLegacyV1ConfigurationAdapters();
#pragma warning restore CS0618

        // ── V2 POCO registrations (section-bind overwrites adapter defaults when V2 JSON exists) ──
        _ = services.Configure<GeneralConfig>(configuration.GetSection(GeneralConfig.Section));

        // MultiPluginConflictOptions: V1 config binds the old section value; V2 has no section → default ThrowError
        _ = services.Configure<MultiPluginConflictOptions>(configuration.GetSection(MultiPluginConflictOptions.Section));
        _ = services.Configure<TemplateManagementConfig>(configuration.GetSection(TemplateManagementConfig.Section));
        _ = services.Configure<RegistrySettingsConfig>(configuration.GetSection(RegistrySettingsConfig.Section));

        // Normalizer: applies TemplateRepository shorthand to individual repository endpoints
        _ = services.AddSingleton<IPostConfigureOptions<TemplateManagementConfig>, TemplateManagementConfigNormalizer>();

        // PluginsConfig: single registration via AddOptions to avoid double-binding of list properties
        _ = services.AddOptions<PluginsConfig>()
            .Bind(configuration.GetSection(PluginsConfig.Section))
            .ValidateOnStart();
        _ = services.AddSingleton<IValidateOptions<PluginsConfig>, PluginsConfigValidator>();

        // ── Resolve fully-populated config for HttpClient registration ──
        // We need TemplateManagementConfig and PluginsConfig to register HttpClients at startup.
        // IOptions<T> is populated by V1 legacy adapters (IConfigureOptions<T>) + V2 section-bind.
        // Since we are still inside DI registration (container not built yet), we build a
        // temporary provider to resolve the options so both V1 and V2 paths are applied.
        using var tempProvider = services.BuildServiceProvider();
        var templateManagement = tempProvider.GetRequiredService<IOptions<TemplateManagementConfig>>().Value;
        var pluginsConfig = tempProvider.GetRequiredService<IOptions<PluginsConfig>>().Value;

        // Template repository HttpClients (AAS, Submodel, ConceptDescription — separate clients)
        _ = services.AddHttpClientWithResilience(AasEnvironmentConfig.AasTemplateRepository, templateManagement.ResiliencePolicies.Retry, templateManagement.AasTemplateRepository.BaseUrl!);
        _ = services.AddHttpClientWithResilience(AasEnvironmentConfig.SubmodelTemplateRepository, templateManagement.ResiliencePolicies.Retry, templateManagement.SubmodelTemplateRepository.BaseUrl!);
        _ = services.AddHttpClientWithResilience(AasEnvironmentConfig.ConceptDescriptorTemplateRepository, templateManagement.ResiliencePolicies.Retry, templateManagement.ConceptDescriptionTemplateRepository.BaseUrl!);

        // Template registry HttpClients (AAS, Submodel)
        _ = services.AddHttpClientWithResilience(AasEnvironmentConfig.AasRegistry, templateManagement.ResiliencePolicies.Retry, templateManagement.AasTemplateRegistry.BaseUrl!);
        _ = services.AddHttpClientWithResilience(AasEnvironmentConfig.SubmodelRegistry, templateManagement.ResiliencePolicies.Retry, templateManagement.SubmodelTemplateRegistry.BaseUrl!);

        // Health check clients (without resilience)
        _ = services.AddHttpClientWithoutResilience(AasEnvironmentConfig.AasTemplateRepositoryHealthCheck, templateManagement.AasTemplateRepository.BaseUrl!);
        _ = services.AddHttpClientWithoutResilience(AasEnvironmentConfig.SubmodelTemplateRepositoryHealthCheck, templateManagement.SubmodelTemplateRepository.BaseUrl!);
        _ = services.AddHttpClientWithoutResilience(AasEnvironmentConfig.ConceptDescriptorTemplateRepositoryHealthCheck, templateManagement.ConceptDescriptionTemplateRepository.BaseUrl!);
        _ = services.AddHttpClientWithoutResilience(AasEnvironmentConfig.AasRegistryHealthCheck, templateManagement.AasTemplateRegistry.BaseUrl!);
        _ = services.AddHttpClientWithoutResilience(AasEnvironmentConfig.SubmodelRegistryHealthCheck, templateManagement.SubmodelTemplateRegistry.BaseUrl!);

        // Plugin HttpClients (from PluginsConfig.Instances)
        if (pluginsConfig.Instances.Count > 0)
        {
            foreach (var plugin in pluginsConfig.Instances)
            {
                _ = services.AddHttpClientWithResilience(PluginConfig.HttpClientNamePrefix + plugin.Name, pluginsConfig.ResiliencePolicies.Retry, plugin.BaseUrl);
                _ = services.AddHttpClientWithoutResilience(PluginConfig.HealthCheckHttpClientNamePrefix + plugin.Name, plugin.BaseUrl!);
            }
        }

        _ = services.AddScoped<IPluginRequestBuilder, PluginRequestBuilder>();
        _ = services.AddScoped<IAasRegistryProvider, AasRegistryProvider>();
        _ = services.AddScoped<ICreateClient, HttpClientFactory>();
        _ = services.AddScoped<IPluginDataProvider, PluginDataProvider>();
        _ = services.AddScoped<IJsonSchemaValidator, JsonSchemaValidator>();
        _ = services.AddScoped<IPluginManifestProvider, PluginManifestProvider>();
        _ = services.AddScoped<IMultiPluginDataHandler, MultiPluginDataHandler>();
        _ = services.AddScoped<ISubmodelDescriptorProvider, SubmodelDescriptorProvider>();
        _ = services.AddSingleton<IPluginManifestHealthStatus, PluginManifestHealthStatus>();
        _ = services.AddHostedService<ShellDescriptorSyncHosted>();
    }
}

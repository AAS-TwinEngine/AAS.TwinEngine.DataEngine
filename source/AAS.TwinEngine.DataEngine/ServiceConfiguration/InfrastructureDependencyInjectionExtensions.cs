using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRegistry.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Helper;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRegistry.Providers;
using AAS.TwinEngine.DataEngine.Infrastructure.Configuration.LegacyV1;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Headers;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Extensions;
using AAS.TwinEngine.DataEngine.Infrastructure.Monitoring;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.AasRegistryProvider.Services;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Helper;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Services;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.SubmodelRegistryProvider.Services;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.TemplateProvider.Services;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config.Helpers;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.ServiceConfiguration;

public static class InfrastructureDependencyInjectionExtensions
{
    public static void ConfigureInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        _ = services.AddHttpClient();

        _ = services.AddScoped<IRequestHeaderMapper, RequestHeaderMapper>();

        _ = services.AddScoped<PluginManifestInitializer>();
        _ = services.AddScoped<ITemplateProvider, TemplateProvider>();
        _ = services.AddScoped<ISubmodelTemplateMappingProvider, SubmodelTemplateMappingProvider>();
        _ = services.AddScoped<IShellTemplateMappingProvider, ShellTemplateMappingProvider>();

        // ── V1 → V2 legacy adapters (IConfigureOptions<T>), no-op when V2 config is present ──
#pragma warning disable CS0618 // Obsolete — intentional V1 backward-compat registration
        _ = services.AddLegacyV1ConfigurationAdapters();
#pragma warning restore CS0618

        // ── V2 POCO registrations (section-bind overwrites adapter defaults when V2 JSON exists) ──
        _ = services.AddOptions<GeneralConfig>()
                .Bind(configuration.GetSection(GeneralConfig.Section))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        // MultiPluginConflictOptions: V1 config binds the old section value; V2 has no section → default ThrowError
        _ = services.Configure<MultiPluginConflictOptions>(configuration.GetSection(MultiPluginConflictOptions.Section));
        _ = services.AddOptions<TemplateManagementConfig>()
            .Bind(configuration.GetSection(TemplateManagementConfig.Section))
            .ValidateOnStart();
        _ = services.AddOptions<RegistrySettingsConfig>()
            .Bind(configuration.GetSection(RegistrySettingsConfig.Section))
            .ValidateOnStart();

        // PluginsConfig: single registration via AddOptions to avoid double-binding of list properties
        _ = services.AddOptions<PluginsConfig>()
            .Bind(configuration.GetSection(PluginsConfig.Section))
            .ValidateOnStart();
        _ = services.AddSingleton<IValidateOptions<PluginsConfig>, PluginsConfigValidator>();

#pragma warning disable CS0618 // Obsolete — intentional V1 backward-compat registration
        _ = services.PostConfigure<PluginsConfig>(options =>
            LegacyPluginsConfigAdapter.ApplyV1PluginInstanceOverrides(configuration, options));
        _ = services.PostConfigure<GeneralConfig>(options =>
            LegacyGeneralConfigAdapter.ApplyV1Overrides(configuration, options));
        _ = services.PostConfigure<TemplateManagementConfig>(options =>
            LegacyTemplateManagementConfigAdapter.ApplyV1Overrides(configuration, options));
        _ = services.PostConfigure<RegistrySettingsConfig>(options =>
            LegacyRegistrySettingsConfigAdapter.ApplyV1Overrides(configuration, options));
#pragma warning restore CS0618

        // Validators
        _ = services.AddSingleton<IValidateOptions<TemplateManagementConfig>, TemplateManagementConfigValidator>();
        _ = services.AddSingleton<IValidateOptions<RegistrySettingsConfig>, RegistrySettingsConfigValidator>();

        // ── Resolve config for HttpClient registration (no BuildServiceProvider) ──
        // Bind V2 sections, apply V1 adapter + normalizer manually.
        var templateManagement = new TemplateManagementConfig();
        configuration.GetSection(TemplateManagementConfig.Section).Bind(templateManagement);
#pragma warning disable CS0618 // Obsolete — intentional V1 backward-compat mapping
        LegacyTemplateManagementConfigAdapter.MapToConfig(configuration, templateManagement);
#pragma warning restore CS0618

        var pluginsConfig = new PluginsConfig();
        configuration.GetSection(PluginsConfig.Section).Bind(pluginsConfig);
#pragma warning disable CS0618 // Obsolete — intentional V1 backward-compat mapping
        LegacyPluginsConfigAdapter.MapToConfig(configuration, pluginsConfig);
#pragma warning restore CS0618

        // Template repository HttpClients (AAS, Submodel, ConceptDescription — separate clients)
        _ = services.AddHttpClientWithResilience(HttpClientNames.AasTemplateRepository, templateManagement.ResiliencePolicies.Retry, templateManagement.AasTemplateRepository.BaseUrl!);
        _ = services.AddHttpClientWithResilience(HttpClientNames.SubmodelTemplateRepository, templateManagement.ResiliencePolicies.Retry, templateManagement.SubmodelTemplateRepository.BaseUrl!);
        _ = services.AddHttpClientWithResilience(HttpClientNames.ConceptDescriptorTemplateRepository, templateManagement.ResiliencePolicies.Retry, templateManagement.ConceptDescriptionTemplateRepository.BaseUrl!);

        // Template registry HttpClients (AAS, Submodel)
        _ = services.AddHttpClientWithResilience(HttpClientNames.AasRegistry, templateManagement.ResiliencePolicies.Retry, templateManagement.AasTemplateRegistry.BaseUrl!);
        _ = services.AddHttpClientWithResilience(HttpClientNames.SubmodelRegistry, templateManagement.ResiliencePolicies.Retry, templateManagement.SubmodelTemplateRegistry.BaseUrl!);

        // Health check clients (without resilience)
        _ = services.AddHttpClientWithoutResilience(HttpClientNames.AasTemplateRepositoryHealthCheck, templateManagement.AasTemplateRepository.BaseUrl!);
        _ = services.AddHttpClientWithoutResilience(HttpClientNames.SubmodelTemplateRepositoryHealthCheck, templateManagement.SubmodelTemplateRepository.BaseUrl!);
        _ = services.AddHttpClientWithoutResilience(HttpClientNames.ConceptDescriptorTemplateRepositoryHealthCheck, templateManagement.ConceptDescriptionTemplateRepository.BaseUrl!);
        _ = services.AddHttpClientWithoutResilience(HttpClientNames.AasRegistryHealthCheck, templateManagement.AasTemplateRegistry.BaseUrl!);
        _ = services.AddHttpClientWithoutResilience(HttpClientNames.SubmodelRegistryHealthCheck, templateManagement.SubmodelTemplateRegistry.BaseUrl!);

        // Plugin HttpClients (from PluginsConfig.Instances)
        if (pluginsConfig.Instances.Count > 0)
        {
            foreach (var plugin in pluginsConfig.Instances)
            {
                _ = services.AddHttpClientWithResilience(HttpClientNames.PluginDataProviderPrefix + plugin.Name, pluginsConfig.ResiliencePolicies.Retry, plugin.BaseUrl);
                _ = services.AddHttpClientWithoutResilience(HttpClientNames.PluginHealthCheckPrefix + plugin.Name, plugin.BaseUrl!);
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

using AAS.TwinEngine.DataEngine.Infrastructure.Configuration.LegacyV1;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Configuration.LegacyV1;

#pragma warning disable CS0618 // Obsolete — testing V1 backward-compat code

/// <summary>
/// End-to-end tests that verify V1 (old) and V2 (new) configurations both produce
/// correctly populated V2 POCO classes through the full DI pipeline
/// (legacy adapters + section binding).
/// </summary>
public class ConfigurationBackwardCompatibilityE2ETests
{
    /// <summary>
    /// Verifies that V1 config → legacy adapters → V2 POCOs works end-to-end.
    /// </summary>
    [Fact]
    public void V1Config_ThroughDIPipeline_ProducesCorrectGeneralConfig()
    {
        var provider = BuildServiceProvider(BuildV1Config());

        var general = provider.GetRequiredService<IOptions<GeneralConfig>>().Value;

        Assert.Equal(new Uri("https://mm-software.com"), general.CustomerDomainUrl);
        Assert.Equal(new Uri("https://localhost:5059"), general.DataEngineRepositoryBaseUrl);
        Assert.Equal("*", general.AllowedHosts);
        Assert.Equal(8192, general.HeaderSanitization.MaxHeaderSize);
        Assert.Equal("http://localhost:4317", general.OpenTelemetry.OtlpEndpoint);
    }

    [Fact]
    public void V1Config_ThroughDIPipeline_ProducesCorrectPluginsConfig()
    {
        var provider = BuildServiceProvider(BuildV1Config());

        var plugins = provider.GetRequiredService<IOptions<PluginsConfig>>().Value;

        Assert.Equal("_aastwinengineindex_", plugins.SubmodelElementIndexContextPrefix);
        Assert.Equal("_", plugins.MultiLanguageProperty.SemanticPostfixSeparator);
        Assert.Contains("de", plugins.MultiLanguageProperty.DefaultLanguages!);
        Assert.Contains("en", plugins.MultiLanguageProperty.DefaultLanguages!);
        Assert.Equal(3, plugins.ResiliencePolicies.Retry.MaxRetryAttempts);
        Assert.Equal(10, plugins.ResiliencePolicies.Retry.DelayInSeconds);
        Assert.Single(plugins.Instances);
        Assert.Equal("Plugin1", plugins.Instances[0].Name);
        Assert.Equal(new Uri("http://localhost:8086"), plugins.Instances[0].BaseUrl);
        Assert.Equal(2, plugins.Instances[0].HeaderMappings.Count);
    }

    [Fact]
    public void V1Config_ThroughDIPipeline_ProducesCorrectTemplateManagementConfig()
    {
        var provider = BuildServiceProvider(BuildV1Config());

        var tmConfig = provider.GetRequiredService<IOptions<TemplateManagementConfig>>().Value;

        Assert.Equal("InternalSemanticId", tmConfig.Semantics.InternalSemanticId);
        Assert.Equal(3, tmConfig.ResiliencePolicies.Retry.MaxRetryAttempts);
        Assert.Equal(10, tmConfig.ResiliencePolicies.Retry.DelayInSeconds);
        Assert.Equal(new Uri("http://localhost:8081"), tmConfig.AasTemplateRepository.BaseUrl);
        Assert.Equal(new Uri("http://localhost:8082"), tmConfig.AasTemplateRegistry.BaseUrl);
        Assert.Equal(new Uri("http://localhost:8083"), tmConfig.SubmodelTemplateRegistry.BaseUrl);
        Assert.NotEmpty(tmConfig.TemplateMappingRules.SubmodelTemplateMappings);
    }

    [Fact]
    public void V1Config_ThroughDIPipeline_ProducesCorrectRegistrySettings()
    {
        var provider = BuildServiceProvider(BuildV1Config());

        var registry = provider.GetRequiredService<IOptions<RegistrySettingsConfig>>().Value;

        Assert.True(registry.PreComputed.Enabled);
        Assert.Equal("0 */3 * * * *", registry.PreComputed.Schedule);
    }

    /// <summary>
    /// Verifies that V2 config → direct section binding → V2 POCOs works end-to-end.
    /// Legacy adapters are registered but should be no-ops.
    /// </summary>
    [Fact]
    public void V2Config_ThroughDIPipeline_ProducesCorrectGeneralConfig()
    {
        var provider = BuildServiceProvider(BuildV2Config());

        var general = provider.GetRequiredService<IOptions<GeneralConfig>>().Value;

        Assert.Equal(new Uri("https://mm-software.com"), general.CustomerDomainUrl);
        Assert.Equal("*", general.AllowedHosts);
        Assert.Equal("http://localhost:4317", general.OpenTelemetry.OtlpEndpoint);
        Assert.Equal("TwinEngine", general.OpenTelemetry.ServiceName);
    }

    [Fact]
    public void V2Config_ThroughDIPipeline_ProducesCorrectPluginsConfig()
    {
        var provider = BuildServiceProvider(BuildV2Config());

        var plugins = provider.GetRequiredService<IOptions<PluginsConfig>>().Value;

        Assert.Equal("_aastwinengineindex_", plugins.SubmodelElementIndexContextPrefix);
        Assert.Equal("_", plugins.MultiLanguageProperty.SemanticPostfixSeparator);
        Assert.Contains("de", plugins.MultiLanguageProperty.DefaultLanguages!);
        Assert.Contains("en", plugins.MultiLanguageProperty.DefaultLanguages!);
        Assert.Equal(4, plugins.ResiliencePolicies.Retry.MaxRetryAttempts);
        Assert.Equal(7, plugins.ResiliencePolicies.Retry.DelayInSeconds);
        Assert.Equal(2.0, plugins.ResiliencePolicies.Retry.BackoffMultiplier);
        Assert.Single(plugins.Instances);
        Assert.Equal("RelationalDatabasePlugin", plugins.Instances[0].Name);
        Assert.Equal(new Uri("http://localhost:8086"), plugins.Instances[0].BaseUrl);
        Assert.Equal(3, plugins.Instances[0].HeaderMappings.Count);
    }

    [Fact]
    public void V2Config_ThroughDIPipeline_ProducesCorrectTemplateManagementConfig()
    {
        var provider = BuildServiceProvider(BuildV2Config());

        var tmConfig = provider.GetRequiredService<IOptions<TemplateManagementConfig>>().Value;

        Assert.Equal("InternalSemanticId", tmConfig.Semantics.InternalSemanticId);
        Assert.Equal(4, tmConfig.ResiliencePolicies.Retry.MaxRetryAttempts);
        Assert.Equal(new Uri("http://localhost:8081"), tmConfig.AasTemplateRepository.BaseUrl);
        Assert.Equal(new Uri("http://localhost:8082"), tmConfig.AasTemplateRegistry.BaseUrl);
        Assert.Equal(new Uri("http://localhost:8083"), tmConfig.SubmodelTemplateRegistry.BaseUrl);
        Assert.NotEmpty(tmConfig.TemplateMappingRules.SubmodelTemplateMappings);
    }

    [Fact]
    public void V2Config_ThroughDIPipeline_RegistrySettingsCorrectSpelling_Works()
    {
        var config = BuildConfig(MergeConfigs(
            GetV2CoreConfig(),
            new Dictionary<string, string?>
            {
                ["RegistrySettings:PreComputed:Enabled"] = "false",
                ["RegistrySettings:PreComputed:Schedule"] = "0 */10 * * * *"
            }));

        var provider = BuildServiceProvider(config);
        var registry = provider.GetRequiredService<IOptions<RegistrySettingsConfig>>().Value;

        Assert.False(registry.PreComputed.Enabled);
        Assert.Equal("0 */10 * * * *", registry.PreComputed.Schedule);
    }

    /// <summary>
    /// Verifies that V1 Semantics section values are correctly split across 
    /// PluginsConfig and TemplateManagementConfig.
    /// </summary>
    [Fact]
    public void V1Config_SemanticsSection_SplitsCorrectlyAcrossV2POCOs()
    {
        var provider = BuildServiceProvider(BuildV1Config());

        var plugins = provider.GetRequiredService<IOptions<PluginsConfig>>().Value;
        var tmConfig = provider.GetRequiredService<IOptions<TemplateManagementConfig>>().Value;

        // SubmodelElementIndexContextPrefix → Plugins
        Assert.Equal("_aastwinengineindex_", plugins.SubmodelElementIndexContextPrefix);
        // MultiLanguageSemanticPostfixSeparator → Plugins:MultiLanguageProperty:SemanticPostfixSeparator
        Assert.Equal("_", plugins.MultiLanguageProperty.SemanticPostfixSeparator);
        // InternalSemanticId → TemplateManagement:Semantics:InternalSemanticId
        Assert.Equal("InternalSemanticId", tmConfig.Semantics.InternalSemanticId);
    }

    /// <summary>
    /// Verifies that V1 HeaderForwarding gets decomposed correctly:
    /// HeaderSanitization → GeneralConfig,
    /// TemplateRepository headers → TemplateManagementConfig endpoints,
    /// Plugin headers → PluginsConfig instances.
    /// </summary>
    [Fact]
    public void V1Config_HeaderForwarding_DecomposesCorrectlyAcrossV2POCOs()
    {
        var provider = BuildServiceProvider(BuildV1Config());

        var general = provider.GetRequiredService<IOptions<GeneralConfig>>().Value;
        var plugins = provider.GetRequiredService<IOptions<PluginsConfig>>().Value;
        var tmConfig = provider.GetRequiredService<IOptions<TemplateManagementConfig>>().Value;

        // HeaderSanitization → General
        Assert.Equal(8192, general.HeaderSanitization.MaxHeaderSize);

        // TemplateRepository headers → TemplateManagement endpoint
        Assert.Equal(2, tmConfig.AasTemplateRepository.HeaderMappings.Count);

        // TemplateRegistry headers → TemplateManagement endpoint
        Assert.Single(tmConfig.AasTemplateRegistry.HeaderMappings);

        // Plugin headers → PluginsConfig instances
        Assert.Equal(2, plugins.Instances[0].HeaderMappings.Count);
        Assert.Equal("Authorization", plugins.Instances[0].HeaderMappings[0].Source);
        Assert.Equal("X-Auth-Token", plugins.Instances[0].HeaderMappings[0].Target);
    }

    /// <summary>
    /// Verifies that V1 HttpRetryPolicyOptions named sections get split correctly:
    /// PluginDataProvider → Plugins:ResiliencePolicies:Retry,
    /// TemplateProvider → TemplateManagement:ResiliencePolicies:Retry.
    /// </summary>
    [Fact]
    public void V1Config_HttpRetryPolicyOptions_SplitsCorrectlyAcrossV2POCOs()
    {
        var provider = BuildServiceProvider(BuildV1Config());

        var plugins = provider.GetRequiredService<IOptions<PluginsConfig>>().Value;
        var tmConfig = provider.GetRequiredService<IOptions<TemplateManagementConfig>>().Value;

        // PluginDataProvider → Plugins resilience
        Assert.Equal(3, plugins.ResiliencePolicies.Retry.MaxRetryAttempts);
        Assert.Equal(10, plugins.ResiliencePolicies.Retry.DelayInSeconds);

        // TemplateProvider → TemplateManagement resilience
        Assert.Equal(3, tmConfig.ResiliencePolicies.Retry.MaxRetryAttempts);
        Assert.Equal(10, tmConfig.ResiliencePolicies.Retry.DelayInSeconds);
    }

    /// <summary>
    /// Verifies that V1 AasEnvironment URLs are decomposed to the right V2 targets:
    /// AasEnvironmentRepositoryBaseUrl → TemplateManagement:AasTemplateRepository:baseUrl,
    /// AasRegistryBaseUrl → TemplateManagement:AasTemplateRegistry:baseUrl,
    /// etc.
    /// </summary>
    [Fact]
    public void V1Config_AasEnvironmentUrls_MapToCorrectV2ServiceEndpoints()
    {
        var provider = BuildServiceProvider(BuildV1Config());

        var general = provider.GetRequiredService<IOptions<GeneralConfig>>().Value;
        var tmConfig = provider.GetRequiredService<IOptions<TemplateManagementConfig>>().Value;

        // CustomerDomainUrl → GeneralConfig
        Assert.Equal(new Uri("https://mm-software.com"), general.CustomerDomainUrl);

        // DataEngineRepositoryBaseUrl → GeneralConfig
        Assert.Equal(new Uri("https://localhost:5059"), general.DataEngineRepositoryBaseUrl);

        // AasEnvironmentRepositoryBaseUrl → TemplateManagement:AasTemplateRepository
        Assert.Equal(new Uri("http://localhost:8081"), tmConfig.AasTemplateRepository.BaseUrl);

        // AasEnvironmentRepositoryBaseUrl → TemplateManagement:SubmodelTemplateRepository
        Assert.Equal(new Uri("http://localhost:8081"), tmConfig.SubmodelTemplateRepository.BaseUrl);

        // AasEnvironmentRepositoryBaseUrl → TemplateManagement:ConceptDescriptionTemplateRepository
        Assert.Equal(new Uri("http://localhost:8081"), tmConfig.ConceptDescriptionTemplateRepository.BaseUrl);

        // AasRegistryBaseUrl → TemplateManagement:AasTemplateRegistry
        Assert.Equal(new Uri("http://localhost:8082"), tmConfig.AasTemplateRegistry.BaseUrl);

        // SubModelRegistryBaseUrl → TemplateManagement:SubmodelTemplateRegistry
        Assert.Equal(new Uri("http://localhost:8083"), tmConfig.SubmodelTemplateRegistry.BaseUrl);
    }

    /// <summary>
    /// Verifies that V1 AasRegistryPreComputed section maps to V2 RegistrySettings
    /// with property renames: IsPreComputed → Enabled, ShellDescriptorCron → Schedule.
    /// </summary>
    [Fact]
    public void V1Config_AasRegistryPreComputed_MapsWithPropertyRenames()
    {
        var provider = BuildServiceProvider(BuildV1Config());

        var registry = provider.GetRequiredService<IOptions<RegistrySettingsConfig>>().Value;

        Assert.True(registry.PreComputed.Enabled);
        Assert.Equal("0 */3 * * * *", registry.PreComputed.Schedule);
    }

    // ────────────────────── Helpers ──────────────────────

    private static ServiceProvider BuildServiceProvider(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddSingleton(configuration);

        // Register legacy adapters (no-op for V2, active for V1)
        services.AddLegacyV1ConfigurationAdapters();

        // Register V2 POCO section-binds (overwrites adapter defaults when V2 JSON exists)
        services.Configure<GeneralConfig>(configuration.GetSection(GeneralConfig.Section));
        services.Configure<PluginsConfig>(configuration.GetSection(PluginsConfig.Section));
        services.Configure<TemplateManagementConfig>(configuration.GetSection(TemplateManagementConfig.Section));
        services.Configure<RegistrySettingsConfig>(configuration.GetSection(RegistrySettingsConfig.Section));

        return services.BuildServiceProvider();
    }

    private static Dictionary<string, string?> GetV2CoreConfig()
    {
        return new Dictionary<string, string?>
        {
            // General
            ["General:AllowedHosts"] = "*",
            ["General:CustomerDomainUrl"] = "https://mm-software.com",
            ["General:OpenTelemetry:OtlpEndpoint"] = "http://localhost:4317",
            ["General:OpenTelemetry:ServiceName"] = "TwinEngine",
            ["General:HeaderSanitization:MaxHeaderSize"] = "8192",
            ["General:HeaderSanitization:MaxHeaderNameSize"] = "256",

            // Plugins
            ["Plugins:SubmodelElementIndexContextPrefix"] = "_aastwinengineindex_",
            ["Plugins:MultiLanguageProperty:DefaultLanguages:0"] = "de",
            ["Plugins:MultiLanguageProperty:DefaultLanguages:1"] = "en",
            ["Plugins:MultiLanguageProperty:SemanticPostfixSeparator"] = "_",
            ["Plugins:ResiliencePolicies:Retry:MaxRetryAttempts"] = "4",
            ["Plugins:ResiliencePolicies:Retry:DelayInSeconds"] = "7",
            ["Plugins:ResiliencePolicies:Retry:BackoffMultiplier"] = "2.0",
            ["Plugins:Instances:0:Name"] = "RelationalDatabasePlugin",
            ["Plugins:Instances:0:baseUrl"] = "http://localhost:8086",
            ["Plugins:Instances:0:headerMappings:0:source"] = "Authorization",
            ["Plugins:Instances:0:headerMappings:0:target"] = "X-Auth-Token",
            ["Plugins:Instances:0:headerMappings:0:required"] = "false",
            ["Plugins:Instances:0:headerMappings:1:source"] = "X-Organization-Id",
            ["Plugins:Instances:0:headerMappings:1:target"] = "X-Tenant-Context",
            ["Plugins:Instances:0:headerMappings:1:required"] = "false",
            ["Plugins:Instances:0:headerMappings:2:source"] = "X-Correlation-Id",
            ["Plugins:Instances:0:headerMappings:2:target"] = "X-Request-Id",
            ["Plugins:Instances:0:headerMappings:2:required"] = "false",

            // TemplateManagement
            ["TemplateManagement:Semantics:InternalSemanticId"] = "InternalSemanticId",
            ["TemplateManagement:ResiliencePolicies:Retry:MaxRetryAttempts"] = "4",
            ["TemplateManagement:ResiliencePolicies:Retry:DelayInSeconds"] = "7",
            ["TemplateManagement:ResiliencePolicies:Retry:BackoffMultiplier"] = "2.0",
            ["TemplateManagement:TemplateMappingRules:SubmodelTemplateMappings:0:templateId"] = "https://example.com/Nameplate",
            ["TemplateManagement:TemplateMappingRules:SubmodelTemplateMappings:0:pattern:0"] = "Nameplate",
            ["TemplateManagement:TemplateMappingRules:ShellTemplateMappings:0:templateId"] = "https://mm-software.com/aas/aasTemplate",
            ["TemplateManagement:TemplateMappingRules:ShellTemplateMappings:0:pattern:0"] = "",
            ["TemplateManagement:TemplateMappingRules:AasIdExtractionRules:0:Strategy"] = "Split",
            ["TemplateManagement:TemplateMappingRules:AasIdExtractionRules:0:Pattern"] = "/",
            ["TemplateManagement:TemplateMappingRules:AasIdExtractionRules:0:Index"] = "5",
            ["TemplateManagement:AasTemplateRepository:Name"] = "AasTemplateRepository",
            ["TemplateManagement:AasTemplateRepository:baseUrl"] = "http://localhost:8081",
            ["TemplateManagement:AasTemplateRepository:headerMappings:0:source"] = "Authorization",
            ["TemplateManagement:AasTemplateRepository:headerMappings:0:target"] = "Authorization",
            ["TemplateManagement:AasTemplateRepository:headerMappings:0:required"] = "false",
            ["TemplateManagement:AasTemplateRepository:headerMappings:1:source"] = "X-User-Roles",
            ["TemplateManagement:AasTemplateRepository:headerMappings:1:target"] = "X-Template-Access-Roles",
            ["TemplateManagement:AasTemplateRepository:headerMappings:1:required"] = "false",
            ["TemplateManagement:SubmodelTemplateRepository:Name"] = "SubmodelTemplateRepository",
            ["TemplateManagement:SubmodelTemplateRepository:baseUrl"] = "http://localhost:8081",
            ["TemplateManagement:AasTemplateRegistry:Name"] = "AasTemplateRegistry",
            ["TemplateManagement:AasTemplateRegistry:baseUrl"] = "http://localhost:8082",
            ["TemplateManagement:AasTemplateRegistry:headerMappings:0:source"] = "Authorization",
            ["TemplateManagement:AasTemplateRegistry:headerMappings:0:target"] = "Authorization",
            ["TemplateManagement:AasTemplateRegistry:headerMappings:0:required"] = "false",
            ["TemplateManagement:SubmodelTemplateRegistry:Name"] = "SubmodelTemplateRegistry",
            ["TemplateManagement:SubmodelTemplateRegistry:baseUrl"] = "http://localhost:8083"
        };
    }

    private static Dictionary<string, string?> MergeConfigs(params Dictionary<string, string?>[] configs)
    {
        var merged = new Dictionary<string, string?>();
        foreach (var config in configs)
        {
            foreach (var kvp in config)
            {
                merged[kvp.Key] = kvp.Value;
            }
        }

        return merged;
    }

    private static IConfiguration BuildV1Config()
    {
        return BuildConfig(new Dictionary<string, string?>
        {
            // ApiConfiguration
            ["ApiConfiguration:BasePath"] = "/api",

            // AasEnvironment
            ["AasEnvironment:DataEngineRepositoryBaseUrl"] = "https://localhost:5059",
            ["AasEnvironment:AasEnvironmentRepositoryBaseUrl"] = "http://localhost:8081",
            ["AasEnvironment:AasRegistryBaseUrl"] = "http://localhost:8082",
            ["AasEnvironment:SubModelRegistryBaseUrl"] = "http://localhost:8083",
            ["AasEnvironment:CustomerDomainUrl"] = "https://mm-software.com",

            // Semantics (split across Plugins + TemplateManagement in V2)
            ["Semantics:MultiLanguageSemanticPostfixSeparator"] = "_",
            ["Semantics:SubmodelElementIndexContextPrefix"] = "_aastwinengineindex_",
            ["Semantics:InternalSemanticId"] = "InternalSemanticId",

            // MultiLanguageProperty
            ["MultiLanguageProperty:DefaultLanguages:0"] = "de",
            ["MultiLanguageProperty:DefaultLanguages:1"] = "en",

            // HttpRetryPolicyOptions (split across Plugins + TemplateManagement in V2)
            ["HttpRetryPolicyOptions:PluginDataProvider:MaxRetryAttempts"] = "3",
            ["HttpRetryPolicyOptions:PluginDataProvider:DelayInSeconds"] = "10",
            ["HttpRetryPolicyOptions:TemplateProvider:MaxRetryAttempts"] = "3",
            ["HttpRetryPolicyOptions:TemplateProvider:DelayInSeconds"] = "10",

            // PluginConfig
            ["PluginConfig:Plugins:0:PluginName"] = "Plugin1",
            ["PluginConfig:Plugins:0:PluginUrl"] = "http://localhost:8086",

            // HeaderForwarding (decomposed across General/Plugins/TemplateManagement in V2)
            ["HeaderForwarding:HeaderSanitization:MaxHeaderSize"] = "8192",
            ["HeaderForwarding:HeaderSanitization:MaxHeaderNameSize"] = "256",
            ["HeaderForwarding:HeaderMappings:TemplateRepository:0:Source"] = "Authorization",
            ["HeaderForwarding:HeaderMappings:TemplateRepository:0:Target"] = "Authorization",
            ["HeaderForwarding:HeaderMappings:TemplateRepository:0:Required"] = "false",
            ["HeaderForwarding:HeaderMappings:TemplateRepository:1:Source"] = "X-User-Roles",
            ["HeaderForwarding:HeaderMappings:TemplateRepository:1:Target"] = "X-Template-Access-Roles",
            ["HeaderForwarding:HeaderMappings:TemplateRepository:1:Required"] = "false",
            ["HeaderForwarding:HeaderMappings:TemplateRegistry:0:Source"] = "Authorization",
            ["HeaderForwarding:HeaderMappings:TemplateRegistry:0:Target"] = "Authorization",
            ["HeaderForwarding:HeaderMappings:TemplateRegistry:0:Required"] = "false",
            ["HeaderForwarding:HeaderMappings:Plugins:Plugin1:0:Source"] = "Authorization",
            ["HeaderForwarding:HeaderMappings:Plugins:Plugin1:0:Target"] = "X-Auth-Token",
            ["HeaderForwarding:HeaderMappings:Plugins:Plugin1:0:Required"] = "false",
            ["HeaderForwarding:HeaderMappings:Plugins:Plugin1:1:Source"] = "X-Organization-Id",
            ["HeaderForwarding:HeaderMappings:Plugins:Plugin1:1:Target"] = "X-Tenant-Context",
            ["HeaderForwarding:HeaderMappings:Plugins:Plugin1:1:Required"] = "false",

            // TemplateMappingRules
            ["TemplateMappingRules:SubmodelTemplateMappings:0:templateId"] = "https://example.com/Nameplate",
            ["TemplateMappingRules:SubmodelTemplateMappings:0:pattern:0"] = "Nameplate",
            ["TemplateMappingRules:ShellTemplateMappings:0:templateId"] = "https://mm-software.com/aas/aasTemplate",
            ["TemplateMappingRules:ShellTemplateMappings:0:pattern:0"] = "",
            ["TemplateMappingRules:AasIdExtractionRules:0:Pattern"] = "Split",
            ["TemplateMappingRules:AasIdExtractionRules:0:Index"] = "6",
            ["TemplateMappingRules:AasIdExtractionRules:0:Separator"] = "/",

            // AasRegistryPreComputed
            ["AasRegistryPreComputed:ShellDescriptorCron"] = "0 */3 * * * *",
            ["AasRegistryPreComputed:IsPreComputed"] = "true",

            // AllowedHosts
            ["AllowedHosts"] = "*",

            // OpenTelemetry
            ["OpenTelemetry:OtlpEndpoint"] = "http://localhost:4317",
            ["OpenTelemetry:ServiceName"] = "TwinEngine",
            ["OpenTelemetry:ServiceVersion"] = "1.0.0"
        });
    }

    private static IConfiguration BuildV2Config()
    {
        return BuildConfig(MergeConfigs(
            GetV2CoreConfig(),
            new Dictionary<string, string?>
            {
                ["RegistrySettings:PreComputed:Enabled"] = "true",
                ["RegistrySettings:PreComputed:Schedule"] = "0 */3 * * * *"
            }));
    }

    private static IConfiguration BuildConfig(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}

using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Providers;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.ModuleTests.Api.Services.ConfigurationMigration;

/// <summary>
/// Module tests that boot the full application with the actual V1 (old) and V2 (new)
/// JSON configuration files and verify that the V2 POCO classes are populated correctly
/// in both cases. This validates the complete backward compatibility pipeline end-to-end
/// through the real DI container and configuration system.
/// </summary>
public class ConfigurationBackwardCompatibilityModuleTests
{
    /// <summary>
    /// Custom factory that boots the app using a specific config directory.
    /// Each directory contains an appsettings.json in either V1 or V2 format.
    /// The content root is set to this directory so WebApplicationBuilder
    /// automatically loads the correct appsettings.json.
    /// </summary>
    /// <param name="configDirName">
    /// Subdirectory name under TestData (e.g. "v1-config" or "v2-config")
    /// that contains the appsettings.json file.
    /// </param>
    private sealed class ConfigTestFactory(string configDirName) : WebApplicationFactory<Program>
    {
        private readonly string _configDir = Path.Combine(AppContext.BaseDirectory, "TestData", configDirName);

        protected override IHost CreateHost(IHostBuilder builder)
        {
            // Point content root to the config directory so the default builder
            // loads its appsettings.json (V1 or V2 format) automatically.
            _ = builder.UseContentRoot(_configDir);
            _ = builder.UseEnvironment("ConfigTest");

            _ = builder.ConfigureServices(services =>
            {
                _ = services.AddSingleton(Substitute.For<ICreateClient>());
                _ = services.AddSingleton(Substitute.For<IPluginManifestProvider>());
                _ = services.AddSingleton(Substitute.For<IPluginManifestConflictHandler>());
            });

            return base.CreateHost(builder);
        }
    }

    // ────────────────────── V1 (Old Config) Tests ──────────────────────

    [Fact]
    public void V1Config_ResolvesGeneralConfig_WithCorrectValues()
    {
        using var appFactory = new ConfigTestFactory("v1-config");
        using var scope = appFactory.Services.CreateScope();

        var general = scope.ServiceProvider.GetRequiredService<IOptions<GeneralConfig>>().Value;

        Assert.Equal(new Uri("https://mm-software.com"), general.CustomerDomainUrl);
        Assert.Equal(new Uri("http://localhost"), general.DataEngineRepositoryBaseUrl);
        Assert.Equal("*", general.AllowedHosts);
        Assert.Equal(8192, general.HeaderSanitization.MaxHeaderSize);
        Assert.Equal(256, general.HeaderSanitization.MaxHeaderNameSize);
        Assert.Equal("http://localhost:4317", general.OpenTelemetry.OtlpEndpoint);
        Assert.Equal("TwinEngine", general.OpenTelemetry.ServiceName);
    }

    [Fact]
    public void V1Config_ResolvesPluginsConfig_WithCorrectValues()
    {
        using var appFactory = new ConfigTestFactory("v1-config");
        using var scope = appFactory.Services.CreateScope();

        var plugins = scope.ServiceProvider.GetRequiredService<IOptions<PluginsConfig>>().Value;

        Assert.Equal("_aastwinengineindex_", plugins.SubmodelElementIndexContextPrefix);
        Assert.Equal("_", plugins.MultiLanguageProperty.SemanticPostfixSeparator);
        Assert.NotNull(plugins.MultiLanguageProperty.DefaultLanguages);
        Assert.Contains("de", plugins.MultiLanguageProperty.DefaultLanguages);
        Assert.Contains("en", plugins.MultiLanguageProperty.DefaultLanguages);
        Assert.Equal(3, plugins.ResiliencePolicies.Retry.MaxRetryAttempts);
        Assert.Equal(10, plugins.ResiliencePolicies.Retry.DelayInSeconds);
        _ = Assert.Single(plugins.Instances);
        Assert.Equal("Plugin1", plugins.Instances[0].Name);
        Assert.Equal(new Uri("http://localhost:8086"), plugins.Instances[0].BaseUrl);
    }

    [Fact]
    public void V1Config_ResolvesPluginHeaderMappings_FromHeaderForwardingSection()
    {
        using var appFactory = new ConfigTestFactory("v1-config");
        using var scope = appFactory.Services.CreateScope();

        var plugins = scope.ServiceProvider.GetRequiredService<IOptions<PluginsConfig>>().Value;

        _ = Assert.Single(plugins.Instances);
        var headers = plugins.Instances[0].HeaderMappings;
        Assert.Equal(2, headers.Count);
        Assert.Equal("Authorization", headers[0].Source);
        Assert.Equal("X-Auth-Token", headers[0].Target);
        Assert.Equal("X-Organization-Id", headers[1].Source);
        Assert.Equal("X-Tenant-Context", headers[1].Target);
    }

    [Fact]
    public void V1Config_ResolvesTemplateManagementConfig_WithCorrectValues()
    {
        using var appFactory = new ConfigTestFactory("v1-config");
        using var scope = appFactory.Services.CreateScope();

        var tmConfig = scope.ServiceProvider.GetRequiredService<IOptions<TemplateManagementConfig>>().Value;

        Assert.Equal("InternalSemanticId", tmConfig.Semantics.InternalSemanticId);
        Assert.Equal(3, tmConfig.ResiliencePolicies.Retry.MaxRetryAttempts);
        Assert.Equal(10, tmConfig.ResiliencePolicies.Retry.DelayInSeconds);
        Assert.Equal(new Uri("http://localhost:8081"), tmConfig.AasTemplateRepository.BaseUrl);
        Assert.Equal(new Uri("http://localhost:8082"), tmConfig.AasTemplateRegistry.BaseUrl);
        Assert.Equal(new Uri("http://localhost:8083"), tmConfig.SubmodelTemplateRegistry.BaseUrl);
        Assert.Equal(3, tmConfig.TemplateMappingRules.SubmodelTemplateMappings.Count);
        _ = Assert.Single(tmConfig.TemplateMappingRules.ShellTemplateMappings);
        _ = Assert.Single(tmConfig.TemplateMappingRules.AasIdExtractionRules);
    }

    [Fact]
    public void V1Config_ResolvesTemplateRepositoryHeaders_FromHeaderForwardingSection()
    {
        using var appFactory = new ConfigTestFactory("v1-config");
        using var scope = appFactory.Services.CreateScope();

        var tmConfig = scope.ServiceProvider.GetRequiredService<IOptions<TemplateManagementConfig>>().Value;

        Assert.Equal(2, tmConfig.AasTemplateRepository.HeaderMappings.Count);
        Assert.Equal("Authorization", tmConfig.AasTemplateRepository.HeaderMappings[0].Source);
        Assert.Equal("Authorization", tmConfig.AasTemplateRepository.HeaderMappings[0].Target);
        Assert.Equal("X-User-Roles", tmConfig.AasTemplateRepository.HeaderMappings[1].Source);
        Assert.Equal("X-Template-Access-Roles", tmConfig.AasTemplateRepository.HeaderMappings[1].Target);

        _ = Assert.Single(tmConfig.AasTemplateRegistry.HeaderMappings);
        Assert.Equal("Authorization", tmConfig.AasTemplateRegistry.HeaderMappings[0].Source);
    }

    [Fact]
    public void V1Config_ResolvesRegistrySettings_WithPropertyRenames()
    {
        using var appFactory = new ConfigTestFactory("v1-config");
        using var scope = appFactory.Services.CreateScope();

        var registry = scope.ServiceProvider.GetRequiredService<IOptions<RegistrySettingsConfig>>().Value;

        // V1: IsPreComputed → V2: Enabled
        Assert.True(registry.PreComputed.Enabled);
        // V1: ShellDescriptorCron → V2: Schedule
        Assert.Equal("0 */3 * * * *", registry.PreComputed.Schedule);
    }

    // ────────────────────── V2 (New Config) Tests ──────────────────────

    [Fact]
    public void V2Config_ResolvesGeneralConfig_WithCorrectValues()
    {
        using var appFactory = new ConfigTestFactory("v2-config");
        using var scope = appFactory.Services.CreateScope();

        var general = scope.ServiceProvider.GetRequiredService<IOptions<GeneralConfig>>().Value;

        Assert.Equal(new Uri("https://mm-software.com"), general.CustomerDomainUrl);
        Assert.Equal("*", general.AllowedHosts);
        Assert.Equal(8192, general.HeaderSanitization.MaxHeaderSize);
        Assert.Equal("http://localhost:4317", general.OpenTelemetry.OtlpEndpoint);
        Assert.Equal("TwinEngine", general.OpenTelemetry.ServiceName);
    }

    [Fact]
    public void V2Config_ResolvesPluginsConfig_WithCorrectValues()
    {
        using var appFactory = new ConfigTestFactory("v2-config");
        using var scope = appFactory.Services.CreateScope();

        var plugins = scope.ServiceProvider.GetRequiredService<IOptions<PluginsConfig>>().Value;

        Assert.Equal("_aastwinengineindex_", plugins.SubmodelElementIndexContextPrefix);
        Assert.Equal("_", plugins.MultiLanguageProperty.SemanticPostfixSeparator);
        Assert.NotNull(plugins.MultiLanguageProperty.DefaultLanguages);
        Assert.Contains("de", plugins.MultiLanguageProperty.DefaultLanguages);
        Assert.Contains("en", plugins.MultiLanguageProperty.DefaultLanguages);
        Assert.Equal(3, plugins.ResiliencePolicies.Retry.MaxRetryAttempts);
        Assert.Equal(10, plugins.ResiliencePolicies.Retry.DelayInSeconds);
        Assert.Equal(2.0, plugins.ResiliencePolicies.Retry.BackoffMultiplier);
        _ = Assert.Single(plugins.Instances);
        Assert.Equal("Plugin1", plugins.Instances[0].Name);
        Assert.Equal(new Uri("http://localhost:8086"), plugins.Instances[0].BaseUrl);
    }

    [Fact]
    public void V2Config_ResolvesPluginHeaderMappings_InlinePerPlugin()
    {
        using var appFactory = new ConfigTestFactory("v2-config");
        using var scope = appFactory.Services.CreateScope();

        var plugins = scope.ServiceProvider.GetRequiredService<IOptions<PluginsConfig>>().Value;

        var headers = plugins.Instances[0].HeaderMappings;
        Assert.Equal(2, headers.Count);
        Assert.Equal("Authorization", headers[0].Source);
        Assert.Equal("X-Auth-Token", headers[0].Target);
        Assert.Equal("X-Organization-Id", headers[1].Source);
        Assert.Equal("X-Tenant-Context", headers[1].Target);
    }

    [Fact]
    public void V2Config_ResolvesTemplateManagementConfig_WithCorrectValues()
    {
        using var appFactory = new ConfigTestFactory("v2-config");
        using var scope = appFactory.Services.CreateScope();

        var tmConfig = scope.ServiceProvider.GetRequiredService<IOptions<TemplateManagementConfig>>().Value;

        Assert.Equal("InternalSemanticId", tmConfig.Semantics.InternalSemanticId);
        Assert.Equal(3, tmConfig.ResiliencePolicies.Retry.MaxRetryAttempts);
        Assert.Equal(10, tmConfig.ResiliencePolicies.Retry.DelayInSeconds);
        Assert.Equal(new Uri("http://localhost:8081"), tmConfig.AasTemplateRepository.BaseUrl);
        Assert.Equal(new Uri("http://localhost:8082"), tmConfig.AasTemplateRegistry.BaseUrl);
        Assert.Equal(new Uri("http://localhost:8083"), tmConfig.SubmodelTemplateRegistry.BaseUrl);
        Assert.Equal(3, tmConfig.TemplateMappingRules.SubmodelTemplateMappings.Count);
        _ = Assert.Single(tmConfig.TemplateMappingRules.ShellTemplateMappings);
        _ = Assert.Single(tmConfig.TemplateMappingRules.AasIdExtractionRules);
    }

    [Fact]
    public void V2Config_ResolvesEndpointHeaders_InlinePerEndpoint()
    {
        using var appFactory = new ConfigTestFactory("v2-config");
        using var scope = appFactory.Services.CreateScope();

        var tmConfig = scope.ServiceProvider.GetRequiredService<IOptions<TemplateManagementConfig>>().Value;

        Assert.Equal(2, tmConfig.AasTemplateRepository.HeaderMappings.Count);
        Assert.Equal("Authorization", tmConfig.AasTemplateRepository.HeaderMappings[0].Source);
        _ = Assert.Single(tmConfig.AasTemplateRegistry.HeaderMappings);
    }

    [Fact]
    public void V2Config_ResolvesRegistrySettings_Directly()
    {
        using var appFactory = new ConfigTestFactory("v2-config");
        using var scope = appFactory.Services.CreateScope();

        var registry = scope.ServiceProvider.GetRequiredService<IOptions<RegistrySettingsConfig>>().Value;

        Assert.True(registry.PreComputed.Enabled);
        Assert.Equal("0 */3 * * * *", registry.PreComputed.Schedule);
    }

    // ────────────────────── Cross-Format Equivalence Tests ──────────────────────

    [Theory]
    [InlineData("v1-config")]
    [InlineData("v2-config")]
    public void BothConfigs_ResolvePluginBaseUrl_ToSameValue(string configFile)
    {
        using var appFactory = new ConfigTestFactory(configFile);
        using var scope = appFactory.Services.CreateScope();

        var plugins = scope.ServiceProvider.GetRequiredService<IOptions<PluginsConfig>>().Value;

        _ = Assert.Single(plugins.Instances);
        Assert.Equal(new Uri("http://localhost:8086"), plugins.Instances[0].BaseUrl);
    }

    [Theory]
    [InlineData("v1-config")]
    [InlineData("v2-config")]
    public void BothConfigs_ResolveTemplateRepositoryBaseUrl_ToSameValue(string configFile)
    {
        using var appFactory = new ConfigTestFactory(configFile);
        using var scope = appFactory.Services.CreateScope();

        var tmConfig = scope.ServiceProvider.GetRequiredService<IOptions<TemplateManagementConfig>>().Value;

        Assert.Equal(new Uri("http://localhost:8081"), tmConfig.AasTemplateRepository.BaseUrl);
        Assert.Equal(new Uri("http://localhost:8082"), tmConfig.AasTemplateRegistry.BaseUrl);
        Assert.Equal(new Uri("http://localhost:8083"), tmConfig.SubmodelTemplateRegistry.BaseUrl);
    }

    [Theory]
    [InlineData("v1-config")]
    [InlineData("v2-config")]
    public void BothConfigs_ResolveSemanticsProperties_ToSameValues(string configFile)
    {
        using var appFactory = new ConfigTestFactory(configFile);
        using var scope = appFactory.Services.CreateScope();

        var plugins = scope.ServiceProvider.GetRequiredService<IOptions<PluginsConfig>>().Value;
        var tmConfig = scope.ServiceProvider.GetRequiredService<IOptions<TemplateManagementConfig>>().Value;

        Assert.Equal("_aastwinengineindex_", plugins.SubmodelElementIndexContextPrefix);
        Assert.Equal("_", plugins.MultiLanguageProperty.SemanticPostfixSeparator);
        Assert.Equal("InternalSemanticId", tmConfig.Semantics.InternalSemanticId);
    }

    [Theory]
    [InlineData("v1-config")]
    [InlineData("v2-config")]
    public void BothConfigs_ResolveMultiLanguagePropertyDefaults_ToSameValues(string configFile)
    {
        using var appFactory = new ConfigTestFactory(configFile);
        using var scope = appFactory.Services.CreateScope();

        var plugins = scope.ServiceProvider.GetRequiredService<IOptions<PluginsConfig>>().Value;

        Assert.NotNull(plugins.MultiLanguageProperty.DefaultLanguages);
        Assert.Equal(2, plugins.MultiLanguageProperty.DefaultLanguages.Count);
        Assert.Contains("de", plugins.MultiLanguageProperty.DefaultLanguages);
        Assert.Contains("en", plugins.MultiLanguageProperty.DefaultLanguages);
    }

    [Theory]
    [InlineData("v1-config")]
    [InlineData("v2-config")]
    public void BothConfigs_ResolveRegistryPreComputed_ToSameValues(string configFile)
    {
        using var appFactory = new ConfigTestFactory(configFile);
        using var scope = appFactory.Services.CreateScope();

        var registry = scope.ServiceProvider.GetRequiredService<IOptions<RegistrySettingsConfig>>().Value;

        Assert.True(registry.PreComputed.Enabled);
        Assert.Equal("0 */3 * * * *", registry.PreComputed.Schedule);
    }

    [Theory]
    [InlineData("v1-config")]
    [InlineData("v2-config")]
    public void BothConfigs_ResolveCustomerDomainUrl_ToSameValue(string configFile)
    {
        using var appFactory = new ConfigTestFactory(configFile);
        using var scope = appFactory.Services.CreateScope();

        var general = scope.ServiceProvider.GetRequiredService<IOptions<GeneralConfig>>().Value;

        Assert.Equal(new Uri("https://mm-software.com"), general.CustomerDomainUrl);
    }

    [Theory]
    [InlineData("v1-config")]
    [InlineData("v2-config")]
    public void BothConfigs_ResolveHeaderSanitization_ToSameValues(string configFile)
    {
        using var appFactory = new ConfigTestFactory(configFile);
        using var scope = appFactory.Services.CreateScope();

        var general = scope.ServiceProvider.GetRequiredService<IOptions<GeneralConfig>>().Value;

        Assert.Equal(8192, general.HeaderSanitization.MaxHeaderSize);
        Assert.Equal(256, general.HeaderSanitization.MaxHeaderNameSize);
    }

    [Theory]
    [InlineData("v1-config")]
    [InlineData("v2-config")]
    public void BothConfigs_ResolveRetryPolicies_ToSameValues(string configFile)
    {
        using var appFactory = new ConfigTestFactory(configFile);
        using var scope = appFactory.Services.CreateScope();

        var plugins = scope.ServiceProvider.GetRequiredService<IOptions<PluginsConfig>>().Value;
        var tmConfig = scope.ServiceProvider.GetRequiredService<IOptions<TemplateManagementConfig>>().Value;

        Assert.Equal(3, plugins.ResiliencePolicies.Retry.MaxRetryAttempts);
        Assert.Equal(10, plugins.ResiliencePolicies.Retry.DelayInSeconds);
        Assert.Equal(3, tmConfig.ResiliencePolicies.Retry.MaxRetryAttempts);
        Assert.Equal(10, tmConfig.ResiliencePolicies.Retry.DelayInSeconds);
    }

    [Theory]
    [InlineData("v1-config")]
    [InlineData("v2-config")]
    public void BothConfigs_ResolveTemplateMappingRules_WithSameCount(string configFile)
    {
        using var appFactory = new ConfigTestFactory(configFile);
        using var scope = appFactory.Services.CreateScope();

        var tmConfig = scope.ServiceProvider.GetRequiredService<IOptions<TemplateManagementConfig>>().Value;

        Assert.Equal(3, tmConfig.TemplateMappingRules.SubmodelTemplateMappings.Count);
        _ = Assert.Single(tmConfig.TemplateMappingRules.ShellTemplateMappings);
        _ = Assert.Single(tmConfig.TemplateMappingRules.AasIdExtractionRules);
    }
}


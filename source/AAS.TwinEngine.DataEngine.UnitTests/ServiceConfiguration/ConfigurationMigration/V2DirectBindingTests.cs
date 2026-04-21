using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.Extensions.Configuration;

namespace AAS.TwinEngine.DataEngine.UnitTests.ServiceConfiguration.ConfigurationMigration;

/// <summary>
/// Tests that V2 (new) configuration JSON binds directly to V2 POCO classes
/// without any adapter/normalizer involvement.
/// </summary>
public class V2DirectBindingTests
{
    [Fact]
    public void GeneralConfig_BindsFromV2Json()
    {
        var config = BuildV2Config();

        var general = new GeneralConfig();
        config.GetSection(GeneralConfig.Section).Bind(general);

        Assert.Equal("*", general.AllowedHosts);
        Assert.Equal(new Uri("https://mm-software.com"), general.CustomerDomainUrl);
        Assert.Equal("http://localhost:4317", general.OpenTelemetry.OtlpEndpoint);
        Assert.Equal("TwinEngine", general.OpenTelemetry.ServiceName);
        Assert.Equal(8192, general.HeaderSanitization.MaxHeaderSize);
        Assert.Equal(256, general.HeaderSanitization.MaxHeaderNameSize);
    }

    [Fact]
    public void PluginsConfig_BindsInstancesFromV2Json()
    {
        var config = BuildV2Config();

        var plugins = new PluginsConfig();
        config.GetSection(PluginsConfig.Section).Bind(plugins);

        Assert.Single(plugins.Instances);
        Assert.Equal("RelationalDatabasePlugin", plugins.Instances[0].Name);
        Assert.Equal(new Uri("http://localhost:8086"), plugins.Instances[0].BaseUrl);
    }

    [Fact]
    public void PluginsConfig_BindsMultiLanguagePropertyFromV2Json()
    {
        var config = BuildV2Config();

        var plugins = new PluginsConfig();
        config.GetSection(PluginsConfig.Section).Bind(plugins);

        Assert.NotNull(plugins.MultiLanguageProperty.DefaultLanguages);
        Assert.Contains("de", plugins.MultiLanguageProperty.DefaultLanguages);
        Assert.Contains("en", plugins.MultiLanguageProperty.DefaultLanguages);
        Assert.Equal("_", plugins.MultiLanguageProperty.SemanticPostfixSeparator);
    }

    [Fact]
    public void PluginsConfig_BindsSubmodelElementIndexContextPrefixFromV2Json()
    {
        var config = BuildV2Config();

        var plugins = new PluginsConfig();
        config.GetSection(PluginsConfig.Section).Bind(plugins);

        Assert.Equal("_aastwinengineindex_", plugins.SubmodelElementIndexContextPrefix);
    }

    [Fact]
    public void PluginsConfig_BindsResiliencePoliciesFromV2Json()
    {
        var config = BuildV2Config();

        var plugins = new PluginsConfig();
        config.GetSection(PluginsConfig.Section).Bind(plugins);

        Assert.Equal(4, plugins.ResiliencePolicies.Retry.MaxRetryAttempts);
        Assert.Equal(7, plugins.ResiliencePolicies.Retry.DelayInSeconds);
        Assert.Equal(2.0, plugins.ResiliencePolicies.Retry.BackoffMultiplier);
    }

    [Fact]
    public void PluginsConfig_BindsPluginHeaderMappingsFromV2Json()
    {
        var config = BuildV2Config();

        var plugins = new PluginsConfig();
        config.GetSection(PluginsConfig.Section).Bind(plugins);

        var headers = plugins.Instances[0].HeaderMappings;
        Assert.Equal(3, headers.Count);
        Assert.Equal("Authorization", headers[0].Source);
        Assert.Equal("X-Auth-Token", headers[0].Target);
    }

    [Fact]
    public void TemplateManagementConfig_BindsSemanticsFromV2Json()
    {
        var config = BuildV2Config();

        var tmConfig = new TemplateManagementConfig();
        config.GetSection(TemplateManagementConfig.Section).Bind(tmConfig);

        Assert.Equal("InternalSemanticId", tmConfig.Semantics.InternalSemanticId);
    }

    [Fact]
    public void TemplateManagementConfig_BindsTemplateMappingRulesFromV2Json()
    {
        var config = BuildV2Config();

        var tmConfig = new TemplateManagementConfig();
        config.GetSection(TemplateManagementConfig.Section).Bind(tmConfig);

        Assert.NotEmpty(tmConfig.TemplateMappingRules.SubmodelTemplateMappings);
        Assert.NotEmpty(tmConfig.TemplateMappingRules.ShellTemplateMappings);
        Assert.NotEmpty(tmConfig.TemplateMappingRules.AasIdExtractionRules);
    }

    [Fact]
    public void TemplateManagementConfig_BindsServiceEndpointsFromV2Json()
    {
        var config = BuildV2Config();

        var tmConfig = new TemplateManagementConfig();
        config.GetSection(TemplateManagementConfig.Section).Bind(tmConfig);

        Assert.Equal(new Uri("http://localhost:8081"), tmConfig.AasTemplateRepository.BaseUrl);
        Assert.Equal(new Uri("http://localhost:8082"), tmConfig.AasTemplateRegistry.BaseUrl);
        Assert.Equal(new Uri("http://localhost:8083"), tmConfig.SubmodelTemplateRegistry.BaseUrl);
        Assert.Equal(new Uri("http://localhost:8081"), tmConfig.SubmodelTemplateRepository.BaseUrl);
    }

    [Fact]
    public void TemplateManagementConfig_BindsResiliencePoliciesFromV2Json()
    {
        var config = BuildV2Config();

        var tmConfig = new TemplateManagementConfig();
        config.GetSection(TemplateManagementConfig.Section).Bind(tmConfig);

        Assert.Equal(4, tmConfig.ResiliencePolicies.Retry.MaxRetryAttempts);
        Assert.Equal(7, tmConfig.ResiliencePolicies.Retry.DelayInSeconds);
        Assert.Equal(2.0, tmConfig.ResiliencePolicies.Retry.BackoffMultiplier);
    }

    [Fact]
    public void TemplateManagementConfig_BindsEndpointHeaderMappingsFromV2Json()
    {
        var config = BuildV2Config();

        var tmConfig = new TemplateManagementConfig();
        config.GetSection(TemplateManagementConfig.Section).Bind(tmConfig);

        Assert.Equal(2, tmConfig.AasTemplateRepository.HeaderMappings.Count);
        Assert.Equal("Authorization", tmConfig.AasTemplateRepository.HeaderMappings[0].Source);
        Assert.Single(tmConfig.AasTemplateRegistry.HeaderMappings);
    }

    [Fact]
    public void RegistrySettingsConfig_BindsFromV2Json()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["RegistrySettings:PreComputed:Enabled"] = "true",
            ["RegistrySettings:PreComputed:Schedule"] = "0 */5 * * * *"
        });

        var registrySettings = new RegistrySettingsConfig();
        config.GetSection(RegistrySettingsConfig.Section).Bind(registrySettings);

        Assert.True(registrySettings.PreComputed.Enabled);
        Assert.Equal("0 */5 * * * *", registrySettings.PreComputed.Schedule);
    }

    private static IConfiguration BuildV2Config()
    {
        return BuildConfig(new Dictionary<string, string?>
        {
            // General
            ["General:AllowedHosts"] = "*",
            ["General:CustomerDomainUrl"] = "https://mm-software.com",
            ["General:OpenTelemetry:OtlpEndpoint"] = "http://localhost:4317",
            ["General:OpenTelemetry:ServiceName"] = "TwinEngine",
            ["General:OpenTelemetry:ServiceVersion"] = "1.0.0",
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
            ["TemplateManagement:TemplateMappingRules:AasIdExtractionRules:0:Pattern"] = "Regex",
            ["TemplateManagement:TemplateMappingRules:AasIdExtractionRules:0:Index"] = "6",
            ["TemplateManagement:TemplateMappingRules:AasIdExtractionRules:0:Separator"] = "/",
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
        });
    }

    private static IConfiguration BuildConfig(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}

using AAS.TwinEngine.DataEngine.Infrastructure.Configuration.LegacyV1;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.Extensions.Configuration;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Configuration.LegacyV1;

#pragma warning disable CS0618 // Obsolete — testing V1 backward-compat code

public class LegacyPluginsConfigAdapterTests
{
    [Fact]
    public void Configure_WithV1Config_MapsSubmodelElementIndexContextPrefix()
    {
        var config = BuildV1Config();
        var adapter = new LegacyPluginsConfigAdapter(config);
        var options = new PluginsConfig();

        adapter.Configure(options);

        Assert.Equal("_aastwinengineindex_", options.SubmodelElementIndexContextPrefix);
    }

    [Fact]
    public void Configure_WithV1Config_MapsSemanticPostfixSeparator()
    {
        var config = BuildV1Config();
        var adapter = new LegacyPluginsConfigAdapter(config);
        var options = new PluginsConfig();

        adapter.Configure(options);

        Assert.Equal("_", options.MultiLanguageProperty.SemanticPostfixSeparator);
    }

    [Fact]
    public void Configure_WithV1Config_MapsDefaultLanguages()
    {
        var config = BuildV1Config();
        var adapter = new LegacyPluginsConfigAdapter(config);
        var options = new PluginsConfig();

        adapter.Configure(options);

        Assert.NotNull(options.MultiLanguageProperty.DefaultLanguages);
        Assert.Equal(2, options.MultiLanguageProperty.DefaultLanguages.Count);
        Assert.Contains("de", options.MultiLanguageProperty.DefaultLanguages);
        Assert.Contains("en", options.MultiLanguageProperty.DefaultLanguages);
    }

    [Fact]
    public void Configure_WithV1Config_MapsRetryPolicy()
    {
        var config = BuildV1Config();
        var adapter = new LegacyPluginsConfigAdapter(config);
        var options = new PluginsConfig();

        adapter.Configure(options);

        Assert.Equal(3, options.ResiliencePolicies.Retry.MaxRetryAttempts);
        Assert.Equal(10, options.ResiliencePolicies.Retry.DelayInSeconds);
    }

    [Fact]
    public void Configure_WithV1Config_MapsPluginInstances()
    {
        var config = BuildV1Config();
        var adapter = new LegacyPluginsConfigAdapter(config);
        var options = new PluginsConfig();

        adapter.Configure(options);

        Assert.Single(options.Instances);
        Assert.Equal("Plugin1", options.Instances[0].Name);
        Assert.Equal(new Uri("http://localhost:8086"), options.Instances[0].BaseUrl);
    }

    [Fact]
    public void Configure_WithV1Config_MapsPluginHeaderMappings()
    {
        var config = BuildV1Config();
        var adapter = new LegacyPluginsConfigAdapter(config);
        var options = new PluginsConfig();

        adapter.Configure(options);

        Assert.Single(options.Instances);
        var pluginHeaders = options.Instances[0].HeaderMappings;
        Assert.Equal(2, pluginHeaders.Count);
        Assert.Equal("Authorization", pluginHeaders[0].Source);
        Assert.Equal("X-Auth-Token", pluginHeaders[0].Target);
    }

    [Fact]
    public void Configure_WithV1Config_MultiplePlugins_MapsAll()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["PluginConfig:Plugins:0:PluginName"] = "PluginA",
            ["PluginConfig:Plugins:0:PluginUrl"] = "http://localhost:8086",
            ["PluginConfig:Plugins:1:PluginName"] = "PluginB",
            ["PluginConfig:Plugins:1:PluginUrl"] = "http://localhost:8087",
            ["Semantics:SubmodelElementIndexContextPrefix"] = "_idx_"
        });

        var adapter = new LegacyPluginsConfigAdapter(config);
        var options = new PluginsConfig();

        adapter.Configure(options);

        Assert.Equal(2, options.Instances.Count);
        Assert.Equal("PluginA", options.Instances[0].Name);
        Assert.Equal("PluginB", options.Instances[1].Name);
        Assert.Equal(new Uri("http://localhost:8086"), options.Instances[0].BaseUrl);
        Assert.Equal(new Uri("http://localhost:8087"), options.Instances[1].BaseUrl);
    }

    [Fact]
    public void Configure_WithV1Config_PluginWithNoHeaderMappings_ReturnsEmptyList()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["PluginConfig:Plugins:0:PluginName"] = "UnknownPlugin",
            ["PluginConfig:Plugins:0:PluginUrl"] = "http://localhost:9999",
            ["HeaderForwarding:HeaderMappings:Plugins:OtherPlugin:0:Source"] = "Auth",
            ["HeaderForwarding:HeaderMappings:Plugins:OtherPlugin:0:Target"] = "X-Auth",
            ["Semantics:SubmodelElementIndexContextPrefix"] = "_idx_"
        });

        var adapter = new LegacyPluginsConfigAdapter(config);
        var options = new PluginsConfig();

        adapter.Configure(options);

        Assert.Single(options.Instances);
        Assert.Empty(options.Instances[0].HeaderMappings);
    }

    [Fact]
    public void Configure_WithV1Config_PreservesSemanticPostfixWhenMlpHasLanguages()
    {
        // Ensures that when MultiLanguageProperty section has DefaultLanguages,
        // the SemanticPostfixSeparator from Semantics section is preserved
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Semantics:MultiLanguageSemanticPostfixSeparator"] = "~",
            ["Semantics:SubmodelElementIndexContextPrefix"] = "_idx_",
            ["MultiLanguageProperty:DefaultLanguages:0"] = "fr",
            ["MultiLanguageProperty:DefaultLanguages:1"] = "es"
        });

        var adapter = new LegacyPluginsConfigAdapter(config);
        var options = new PluginsConfig();

        adapter.Configure(options);

        Assert.Equal("~", options.MultiLanguageProperty.SemanticPostfixSeparator);
        Assert.Equal(new[] { "fr", "es" }, options.MultiLanguageProperty.DefaultLanguages);
    }

    [Fact]
    public void Configure_WithV2Config_DoesNotModifyOptions()
    {
        var v2Config = BuildConfig(new Dictionary<string, string?>
        {
            ["General:AllowedHosts"] = "*",
            ["Plugins:Instances:0:Name"] = "V2Plugin",
            ["Plugins:Instances:0:baseUrl"] = "http://localhost:9090"
        });

        var adapter = new LegacyPluginsConfigAdapter(v2Config);
        var options = new PluginsConfig();
        var originalPrefix = options.SubmodelElementIndexContextPrefix;

        adapter.Configure(options);

        Assert.Equal(originalPrefix, options.SubmodelElementIndexContextPrefix);
        Assert.Empty(options.Instances);
    }

    private static IConfiguration BuildV1Config()
    {
        return BuildConfig(new Dictionary<string, string?>
        {
            ["Semantics:MultiLanguageSemanticPostfixSeparator"] = "_",
            ["Semantics:SubmodelElementIndexContextPrefix"] = "_aastwinengineindex_",
            ["Semantics:InternalSemanticId"] = "InternalSemanticId",
            ["MultiLanguageProperty:DefaultLanguages:0"] = "de",
            ["MultiLanguageProperty:DefaultLanguages:1"] = "en",
            ["HttpRetryPolicyOptions:PluginDataProvider:MaxRetryAttempts"] = "3",
            ["HttpRetryPolicyOptions:PluginDataProvider:DelayInSeconds"] = "10",
            ["PluginConfig:Plugins:0:PluginName"] = "Plugin1",
            ["PluginConfig:Plugins:0:PluginUrl"] = "http://localhost:8086",
            ["HeaderForwarding:HeaderMappings:Plugins:Plugin1:0:Source"] = "Authorization",
            ["HeaderForwarding:HeaderMappings:Plugins:Plugin1:0:Target"] = "X-Auth-Token",
            ["HeaderForwarding:HeaderMappings:Plugins:Plugin1:0:Required"] = "false",
            ["HeaderForwarding:HeaderMappings:Plugins:Plugin1:1:Source"] = "X-Organization-Id",
            ["HeaderForwarding:HeaderMappings:Plugins:Plugin1:1:Target"] = "X-Tenant-Context",
            ["HeaderForwarding:HeaderMappings:Plugins:Plugin1:1:Required"] = "false"
        });
    }

    private static IConfiguration BuildConfig(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}

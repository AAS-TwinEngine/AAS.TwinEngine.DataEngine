using AAS.TwinEngine.DataEngine.Infrastructure.Configuration.LegacyV1;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.Extensions.Configuration;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Configuration.LegacyV1;

#pragma warning disable CS0618 // Obsolete — testing V1 backward-compat code

public class LegacyTemplateManagementConfigAdapterTests
{
    [Fact]
    public void Configure_WithV1Config_MapsInternalSemanticId()
    {
        var config = BuildV1Config();
        var adapter = new LegacyTemplateManagementConfigAdapter(config);
        var options = new TemplateManagementConfig();

        adapter.Configure(options);

        Assert.Equal("InternalSemanticId", options.Semantics.InternalSemanticId);
    }

    [Fact]
    public void Configure_WithV1Config_MapsTemplateMappingRules()
    {
        var config = BuildV1Config();
        var adapter = new LegacyTemplateManagementConfigAdapter(config);
        var options = new TemplateManagementConfig();

        adapter.Configure(options);

        Assert.Equal(2, options.TemplateMappingRules.SubmodelTemplateMappings.Count);
        Assert.Equal("https://example.com/template1", options.TemplateMappingRules.SubmodelTemplateMappings[0].TemplateId);
        Assert.Contains("Nameplate", options.TemplateMappingRules.SubmodelTemplateMappings[0].Pattern);
    }

    [Fact]
    public void Configure_WithV1Config_MapsShellTemplateMappings()
    {
        var config = BuildV1Config();
        var adapter = new LegacyTemplateManagementConfigAdapter(config);
        var options = new TemplateManagementConfig();

        adapter.Configure(options);

        Assert.Single(options.TemplateMappingRules.ShellTemplateMappings);
        Assert.Equal("https://mm-software.com/aas/aasTemplate", options.TemplateMappingRules.ShellTemplateMappings[0].TemplateId);
    }

    [Fact]
    public void Configure_WithV1Config_MapsRetryPolicy()
    {
        var config = BuildV1Config();
        var adapter = new LegacyTemplateManagementConfigAdapter(config);
        var options = new TemplateManagementConfig();

        adapter.Configure(options);

        Assert.Equal(3, options.ResiliencePolicies.Retry.MaxRetryAttempts);
        Assert.Equal(10, options.ResiliencePolicies.Retry.DelayInSeconds);
    }

    [Fact]
    public void Configure_WithV1Config_MapsAasTemplateRepository()
    {
        var config = BuildV1Config();
        var adapter = new LegacyTemplateManagementConfigAdapter(config);
        var options = new TemplateManagementConfig();

        adapter.Configure(options);

        // V1 maps AasEnvironmentRepositoryBaseUrl to all 3 repository endpoints directly
        Assert.Equal(HttpClientNames.AasTemplateRepository, options.AasTemplateRepository.Name);
        Assert.Equal(new Uri("http://localhost:8081"), options.AasTemplateRepository.BaseUrl);
    }

    [Fact]
    public void Configure_WithV1Config_MapsAasTemplateRegistry()
    {
        var config = BuildV1Config();
        var adapter = new LegacyTemplateManagementConfigAdapter(config);
        var options = new TemplateManagementConfig();

        adapter.Configure(options);

        Assert.Equal(HttpClientNames.AasRegistry, options.AasTemplateRegistry.Name);
        Assert.Equal(new Uri("http://localhost:8082"), options.AasTemplateRegistry.BaseUrl);
    }

    [Fact]
    public void Configure_WithV1Config_MapsSubmodelTemplateRegistry()
    {
        var config = BuildV1Config();
        var adapter = new LegacyTemplateManagementConfigAdapter(config);
        var options = new TemplateManagementConfig();

        adapter.Configure(options);

        Assert.Equal(HttpClientNames.SubmodelRegistry, options.SubmodelTemplateRegistry.Name);
        Assert.Equal(new Uri("http://localhost:8083"), options.SubmodelTemplateRegistry.BaseUrl);
    }

    [Fact]
    public void Configure_WithV1Config_MapsSubmodelTemplateRepository()
    {
        var config = BuildV1Config();
        var adapter = new LegacyTemplateManagementConfigAdapter(config);
        var options = new TemplateManagementConfig();

        adapter.Configure(options);

        // SubmodelTemplateRepository is populated directly from AasEnvironmentRepositoryBaseUrl
        Assert.Equal(HttpClientNames.SubmodelTemplateRepository, options.SubmodelTemplateRepository.Name);
        Assert.Equal(new Uri("http://localhost:8081"), options.SubmodelTemplateRepository.BaseUrl);
    }

    [Fact]
    public void Configure_WithV1Config_MapsTemplateRepositoryHeaders()
    {
        var config = BuildV1Config();
        var adapter = new LegacyTemplateManagementConfigAdapter(config);
        var options = new TemplateManagementConfig();

        adapter.Configure(options);

        // Headers now live on each repository endpoint directly
        Assert.Equal(2, options.AasTemplateRepository.HeaderMappings.Count);
        Assert.Equal("Authorization", options.AasTemplateRepository.HeaderMappings[0].Source);
        Assert.Equal("Authorization", options.AasTemplateRepository.HeaderMappings[0].Target);
    }

    [Fact]
    public void Configure_WithV1Config_MapsTemplateRegistryHeaders()
    {
        var config = BuildV1Config();
        var adapter = new LegacyTemplateManagementConfigAdapter(config);
        var options = new TemplateManagementConfig();

        adapter.Configure(options);

        Assert.Single(options.AasTemplateRegistry.HeaderMappings);
        Assert.Equal("Authorization", options.AasTemplateRegistry.HeaderMappings[0].Source);
    }

    [Fact]
    public void Configure_WithV1Config_MapsAasIdExtractionRules()
    {
        var config = BuildV1Config();
        var adapter = new LegacyTemplateManagementConfigAdapter(config);
        var options = new TemplateManagementConfig();

        adapter.Configure(options);

        Assert.Single(options.TemplateMappingRules.AasIdExtractionRules);
        Assert.Equal("/", options.TemplateMappingRules.AasIdExtractionRules[0].Pattern);
        Assert.Equal(6, options.TemplateMappingRules.AasIdExtractionRules[0].Index);
        Assert.Equal(ExtractionStrategy.Split, options.TemplateMappingRules.AasIdExtractionRules[0].Strategy);
    }

    [Fact]
    public void Configure_WithV2Config_DoesNotModifyOptions()
    {
        var v2Config = BuildConfig(new Dictionary<string, string?>
        {
            ["General:AllowedHosts"] = "*",
            ["TemplateManagement:Semantics:InternalSemanticId"] = "V2Id"
        });

        var adapter = new LegacyTemplateManagementConfigAdapter(v2Config);
        var options = new TemplateManagementConfig();
        var originalSemanticId = options.Semantics.InternalSemanticId;

        adapter.Configure(options);

        Assert.Equal(originalSemanticId, options.Semantics.InternalSemanticId);
    }

    private static IConfiguration BuildV1Config()
    {
        return BuildConfig(new Dictionary<string, string?>
        {
            ["Semantics:InternalSemanticId"] = "InternalSemanticId",
            ["Semantics:SubmodelElementIndexContextPrefix"] = "_aastwinengineindex_",
            ["TemplateMappingRules:SubmodelTemplateMappings:0:templateId"] = "https://example.com/template1",
            ["TemplateMappingRules:SubmodelTemplateMappings:0:pattern:0"] = "Nameplate",
            ["TemplateMappingRules:SubmodelTemplateMappings:1:templateId"] = "https://example.com/template2",
            ["TemplateMappingRules:SubmodelTemplateMappings:1:pattern:0"] = "CarbonFootprint",
            ["TemplateMappingRules:ShellTemplateMappings:0:templateId"] = "https://mm-software.com/aas/aasTemplate",
            ["TemplateMappingRules:ShellTemplateMappings:0:pattern:0"] = "",
            ["TemplateMappingRules:AasIdExtractionRules:0:Pattern"] = "Split",
            ["TemplateMappingRules:AasIdExtractionRules:0:Index"] = "6",
            ["TemplateMappingRules:AasIdExtractionRules:0:Separator"] = "/",
            ["HttpRetryPolicyOptions:TemplateProvider:MaxRetryAttempts"] = "3",
            ["HttpRetryPolicyOptions:TemplateProvider:DelayInSeconds"] = "10",
            ["AasEnvironment:AasEnvironmentRepositoryBaseUrl"] = "http://localhost:8081",
            ["AasEnvironment:AasRegistryBaseUrl"] = "http://localhost:8082",
            ["AasEnvironment:SubModelRegistryBaseUrl"] = "http://localhost:8083",
            ["AasEnvironment:CustomerDomainUrl"] = "https://mm-software.com",
            ["AasEnvironment:DataEngineRepositoryBaseUrl"] = "https://localhost:5059",
            ["HeaderForwarding:HeaderMappings:TemplateRepository:0:Source"] = "Authorization",
            ["HeaderForwarding:HeaderMappings:TemplateRepository:0:Target"] = "Authorization",
            ["HeaderForwarding:HeaderMappings:TemplateRepository:0:Required"] = "false",
            ["HeaderForwarding:HeaderMappings:TemplateRepository:1:Source"] = "X-User-Roles",
            ["HeaderForwarding:HeaderMappings:TemplateRepository:1:Target"] = "X-Template-Access-Roles",
            ["HeaderForwarding:HeaderMappings:TemplateRepository:1:Required"] = "false",
            ["HeaderForwarding:HeaderMappings:TemplateRegistry:0:Source"] = "Authorization",
            ["HeaderForwarding:HeaderMappings:TemplateRegistry:0:Target"] = "Authorization",
            ["HeaderForwarding:HeaderMappings:TemplateRegistry:0:Required"] = "false"
        });
    }

    private static IConfiguration BuildConfig(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}

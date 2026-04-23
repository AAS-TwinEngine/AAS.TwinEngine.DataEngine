using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.Infrastructure.Configuration.LegacyV1;

using Microsoft.Extensions.Configuration;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Configuration.LegacyV1;

#pragma warning disable CS0618 // Obsolete — testing V1 backward-compat code

public class LegacyConfigurationDetectorTests
{
    [Fact]
    public void IsV1Configuration_WithGeneralSection_ReturnsFalse()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["General:AllowedHosts"] = "*"
        });

        Assert.False(LegacyConfigurationDetector.IsV1Configuration(config));
    }

    [Fact]
    public void IsV1Configuration_WithPluginsInstances_ReturnsFalse()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Plugins:Instances:0:Name"] = "TestPlugin"
        });

        Assert.False(LegacyConfigurationDetector.IsV1Configuration(config));
    }

    [Fact]
    public void IsV1Configuration_WithTemplateManagementSection_ReturnsFalse()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["TemplateManagement:Semantics:InternalSemanticId"] = "test"
        });

        Assert.False(LegacyConfigurationDetector.IsV1Configuration(config));
    }

    [Fact]
    public void IsV1Configuration_WithOldFlatConfig_ReturnsTrue()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["PluginConfig:Plugins:0:PluginName"] = "Plugin1",
            ["AasEnvironment:AasRegistryBaseUrl"] = "http://localhost:8082",
            ["Semantics:InternalSemanticId"] = "InternalSemanticId"
        });

        Assert.True(LegacyConfigurationDetector.IsV1Configuration(config));
    }

    [Fact]
    public void IsV1Configuration_EmptyConfig_ReturnsTrue()
    {
        var config = BuildConfig([]);

        Assert.True(LegacyConfigurationDetector.IsV1Configuration(config));
    }

    [Fact]
    public void IsV1Configuration_NullConfig_ThrowsInvalidDependencyException() => Assert.Throws<InvalidDependencyException>(() => LegacyConfigurationDetector.IsV1Configuration(null!));

    private static IConfiguration BuildConfig(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}

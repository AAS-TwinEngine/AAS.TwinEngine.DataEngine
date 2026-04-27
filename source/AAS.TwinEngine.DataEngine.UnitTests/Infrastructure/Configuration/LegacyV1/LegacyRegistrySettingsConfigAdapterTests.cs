using AAS.TwinEngine.DataEngine.Infrastructure.Configuration.LegacyV1;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.Extensions.Configuration;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Configuration.LegacyV1;

#pragma warning disable CS0618 // Obsolete — testing V1 backward-compat code

public class LegacyRegistrySettingsConfigAdapterTests
{
    [Fact]
    public void Configure_WithV1Config_MapsPreComputedEnabled()
    {
        var config = BuildV1Config(isPreComputed: true);
        var adapter = new LegacyRegistrySettingsConfigAdapter(config);
        var options = new RegistrySettingsConfig();

        adapter.Configure(options);

        Assert.True(options.PreComputed.Enabled);
    }

    [Fact]
    public void Configure_WithV1Config_MapsPreComputedDisabled()
    {
        var config = BuildV1Config(isPreComputed: false);
        var adapter = new LegacyRegistrySettingsConfigAdapter(config);
        var options = new RegistrySettingsConfig();

        adapter.Configure(options);

        Assert.False(options.PreComputed.Enabled);
    }

    [Fact]
    public void Configure_WithV1Config_MapsSchedule()
    {
        var config = BuildV1Config();
        var adapter = new LegacyRegistrySettingsConfigAdapter(config);
        var options = new RegistrySettingsConfig();

        adapter.Configure(options);

        Assert.Equal("0 */3 * * * *", options.PreComputed.Schedule);
    }

    [Fact]
    public void Configure_WithV1Config_CustomSchedule()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["AasRegistryPreComputed:ShellDescriptorCron"] = "0 */5 * * * *",
            ["AasRegistryPreComputed:IsPreComputed"] = "true"
        });

        var adapter = new LegacyRegistrySettingsConfigAdapter(config);
        var options = new RegistrySettingsConfig();

        adapter.Configure(options);

        Assert.Equal("0 */5 * * * *", options.PreComputed.Schedule);
        Assert.True(options.PreComputed.Enabled);
    }

    [Fact]
    public void Configure_WithV2Config_DoesNotModifyOptions()
    {
        var v2Config = BuildConfig(new Dictionary<string, string?>
        {
            ["General:AllowedHosts"] = "*",
            ["RegistrySettings:PreComputed:Enabled"] = "true",
            ["RegistrySettings:PreComputed:Schedule"] = "0 */10 * * * *"
        });

        var adapter = new LegacyRegistrySettingsConfigAdapter(v2Config);
        var options = new RegistrySettingsConfig();

        adapter.Configure(options);

        // Adapter is no-op for V2 — options remain at defaults
        Assert.False(options.PreComputed.Enabled);
    }

    private static IConfiguration BuildV1Config(bool isPreComputed = true)
    {
        return BuildConfig(new Dictionary<string, string?>
        {
            ["AasRegistryPreComputed:ShellDescriptorCron"] = "0 */3 * * * *",
            ["AasRegistryPreComputed:IsPreComputed"] = isPreComputed.ToString().ToLowerInvariant()
        });
    }

    private static IConfiguration BuildConfig(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}

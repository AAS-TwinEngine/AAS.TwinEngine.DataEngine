using AAS.TwinEngine.DataEngine.Infrastructure.Configuration.LegacyV1;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.Extensions.Configuration;

namespace AAS.TwinEngine.DataEngine.UnitTests.ServiceConfiguration.ConfigurationMigration;

#pragma warning disable CS0618 // Obsolete — testing V1 backward-compat code

public class LegacyGeneralConfigAdapterTests
{
    [Fact]
    public void Configure_WithV1Config_MapsApiConfiguration()
    {
        var config = BuildV1Config();
        var adapter = new LegacyGeneralConfigAdapter(config);
        var options = new GeneralConfig();

        adapter.Configure(options);

        Assert.Equal("/api", options.ApiConfiguration.BasePath);
    }

    [Fact]
    public void Configure_WithV1Config_MapsCustomerDomainUrl()
    {
        var config = BuildV1Config();
        var adapter = new LegacyGeneralConfigAdapter(config);
        var options = new GeneralConfig();

        adapter.Configure(options);

        Assert.Equal(new Uri("https://mm-software.com"), options.CustomerDomainUrl);
    }

    [Fact]
    public void Configure_WithV1Config_MapsDataEngineRepositoryBaseUrl()
    {
        var config = BuildV1Config();
        var adapter = new LegacyGeneralConfigAdapter(config);
        var options = new GeneralConfig();

        adapter.Configure(options);

        Assert.Equal(new Uri("https://localhost:5059"), options.DataEngineRepositoryBaseUrl);
    }

    [Fact]
    public void Configure_WithV1Config_MapsHeaderSanitization()
    {
        var config = BuildV1Config();
        var adapter = new LegacyGeneralConfigAdapter(config);
        var options = new GeneralConfig();

        adapter.Configure(options);

        Assert.Equal(8192, options.HeaderSanitization.MaxHeaderSize);
        Assert.Equal(256, options.HeaderSanitization.MaxHeaderNameSize);
    }

    [Fact]
    public void Configure_WithV1Config_MapsAllowedHosts()
    {
        var config = BuildV1Config();
        var adapter = new LegacyGeneralConfigAdapter(config);
        var options = new GeneralConfig();

        adapter.Configure(options);

        Assert.Equal("*", options.AllowedHosts);
    }

    [Fact]
    public void Configure_WithV1Config_MapsOpenTelemetry()
    {
        var config = BuildV1Config();
        var adapter = new LegacyGeneralConfigAdapter(config);
        var options = new GeneralConfig();

        adapter.Configure(options);

        Assert.Equal("http://localhost:4317", options.OpenTelemetry.OtlpEndpoint);
        Assert.Equal("TwinEngine", options.OpenTelemetry.ServiceName);
    }

    [Fact]
    public void Configure_WithV2Config_DoesNotModifyOptions()
    {
        var v2Config = BuildConfig(new Dictionary<string, string?>
        {
            ["General:AllowedHosts"] = "example.com",
            ["General:OpenTelemetry:ServiceName"] = "V2Service"
        });

        var adapter = new LegacyGeneralConfigAdapter(v2Config);
        var options = new GeneralConfig();
        var originalAllowedHosts = options.AllowedHosts;

        adapter.Configure(options);

        // Options should remain at defaults since adapter is a no-op for V2
        Assert.Equal(originalAllowedHosts, options.AllowedHosts);
    }

    private static IConfiguration BuildV1Config()
    {
        return BuildConfig(new Dictionary<string, string?>
        {
            ["ApiConfiguration:BasePath"] = "/api",
            ["AasEnvironment:CustomerDomainUrl"] = "https://mm-software.com",
            ["AasEnvironment:DataEngineRepositoryBaseUrl"] = "https://localhost:5059",
            ["AasEnvironment:AasEnvironmentRepositoryBaseUrl"] = "http://localhost:8081",
            ["AasEnvironment:AasRegistryBaseUrl"] = "http://localhost:8082",
            ["AasEnvironment:SubModelRegistryBaseUrl"] = "http://localhost:8083",
            ["HeaderForwarding:HeaderSanitization:MaxHeaderSize"] = "8192",
            ["HeaderForwarding:HeaderSanitization:MaxHeaderNameSize"] = "256",
            ["AllowedHosts"] = "*",
            ["OpenTelemetry:OtlpEndpoint"] = "http://localhost:4317",
            ["OpenTelemetry:ServiceName"] = "TwinEngine",
            ["OpenTelemetry:ServiceVersion"] = "1.0.0"
        });
    }

    private static IConfiguration BuildConfig(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}

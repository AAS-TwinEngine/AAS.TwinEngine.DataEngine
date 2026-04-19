using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Configuration.LegacyV1;

/// <summary>
/// Detects whether the running configuration uses the V1 (flat sections) or V2 (grouped) schema.
/// V2 is identified by the existence of "General", "Plugins:Instances", or "TemplateManagement" top-level sections.
/// </summary>
#pragma warning disable S1133 
[Obsolete("V1 configuration is deprecated and will be removed in next major release")]
public static class LegacyConfigurationDetector
{
    public static bool IsV1Configuration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // V2 introduces these grouped top-level sections; if any exists → V2
        var isV2 = configuration.GetSection(GeneralConfig.Section).Exists()
                || configuration.GetSection("Plugins:Instances").Exists()
                || configuration.GetSection(TemplateManagementConfig.Section).Exists();

        return !isV2;
    }
}

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Serilog;

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
        if (configuration == null)
        {
            throw new InvalidDependencyException(nameof(configuration));
        }

        // V2 introduces these grouped top-level sections; if any exists → V2
        var isV2 = configuration.GetSection(GeneralConfig.Section).Exists()
                || configuration.GetSection("Plugins:Instances").Exists()
                || configuration.GetSection(TemplateManagementConfig.Section).Exists();

        return !isV2;
    }

    public static void WarnIfPreComputedConfigurationDetected(IConfiguration configuration)
    {
        var v2Section = configuration.GetSection("RegistrySettings:PreComputed");
        var v1Section = configuration.GetSection("AasRegistryPreComputed");

        if (v2Section.Exists() || v1Section.Exists())
        {
            Log.Warning(
                "Detected a precomputed configuration section ('RegistrySettings:PreComputed' or 'AasRegistryPreComputed') " +
                "in your settings. The precomputed flow has been removed from DataEngine. " +
                "Please remove the precomputed section from your configuration to suppress this warning.");
        }
    }
}

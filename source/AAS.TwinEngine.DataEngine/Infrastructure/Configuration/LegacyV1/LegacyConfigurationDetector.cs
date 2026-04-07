using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Configuration.LegacyV1;

/// <summary>
/// Detects whether the running configuration uses the V1 (flat sections) or V2 (grouped) schema.
/// V2 is identified by the existence of "General", "Plugins:Instances", or "TemplateManagement" top-level sections.
/// </summary>
[Obsolete("Remove in v2.0.0 — V1 configuration support will be dropped.")]
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

    /// <summary>
    /// The V2 JSON may contain a typo: "ResgistrySettings" instead of "RegistrySettings".
    /// If only the typo variant exists, we still treat it as V2 but need to handle the rename.
    /// </summary>
    public static bool HasRegistrySettingsTypo(IConfiguration configuration)
    {
        return !configuration.GetSection(RegistrySettingsConfig.Section).Exists()
            && configuration.GetSection(RegistrySettingsConfig.SectionTypoVariant).Exists();
    }
}

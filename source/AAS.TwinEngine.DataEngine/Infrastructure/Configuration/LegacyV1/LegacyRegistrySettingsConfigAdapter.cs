using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Configuration.LegacyV1;

/// <summary>
/// Reads V1 flat config sections and maps them into the V2 <see cref="RegistrySettingsConfig"/> shape.
/// </summary>
[Obsolete("Remove in v2.0.0 — V1 configuration support will be dropped.")]
public sealed class LegacyRegistrySettingsConfigAdapter(IConfiguration configuration) : IConfigureOptions<RegistrySettingsConfig>
{
    private readonly IConfiguration _configuration = configuration;

    public void Configure(RegistrySettingsConfig options)
    {
        if (!LegacyConfigurationDetector.IsV1Configuration(_configuration))
        {
            // Even for V2, handle the typo variant "ResgistrySettings"
            if (LegacyConfigurationDetector.HasRegistrySettingsTypo(_configuration))
            {
                var typoSection = _configuration.GetSection(RegistrySettingsConfig.SectionTypoVariant).Get<RegistrySettingsConfig>();
                if (typoSection != null)
                {
                    options.PreComputed = typoSection.PreComputed;
                }
            }

            return;
        }

        // V1: "AasRegistryPreComputed" → V2: "RegistrySettings:PreComputed"
        var preComputed = _configuration.GetSection(AasRegistryPreComputed.Section).Get<AasRegistryPreComputed>();
        if (preComputed != null)
        {
            options.PreComputed = new PreComputedConfig
            {
                Enabled = preComputed.IsPreComputed,
                Schedule = preComputed.ShellDescriptorCron
            };
        }
    }
}

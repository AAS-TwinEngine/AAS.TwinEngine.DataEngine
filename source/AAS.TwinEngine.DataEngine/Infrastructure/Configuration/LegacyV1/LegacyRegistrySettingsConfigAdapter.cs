using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Configuration.LegacyV1;

/// <summary>
/// Reads V1 flat config sections and maps them into the V2 <see cref="RegistrySettingsConfig"/> shape.
/// </summary>
#pragma warning disable S1133 
[Obsolete("V1 configuration is deprecated and will be removed in next major release")]
public sealed class LegacyRegistrySettingsConfigAdapter(IConfiguration configuration) : IConfigureOptions<RegistrySettingsConfig>
{
    private readonly IConfiguration _configuration = configuration;

    public void Configure(RegistrySettingsConfig options)
    {
        if (!LegacyConfigurationDetector.IsV1Configuration(_configuration))
        {
            ApplyV1Overrides(_configuration, options);
            return;
        }

        // V1: "AasRegistryPreComputed" → V2: "RegistrySettings:PreComputed"
        ApplyV1Overrides(_configuration, options);
    }

    /// <summary>
    /// If the V1 <c>AasRegistryPreComputed</c> section exists, overrides the corresponding V2 values.
    /// </summary>
    public static void ApplyV1Overrides(IConfiguration configuration, RegistrySettingsConfig options)
    {
        if (!configuration.GetSection(AasRegistryPreComputed.Section).Exists())
        {
            return;
        }

        var preComputed = configuration.GetSection(AasRegistryPreComputed.Section).Get<AasRegistryPreComputed>();
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

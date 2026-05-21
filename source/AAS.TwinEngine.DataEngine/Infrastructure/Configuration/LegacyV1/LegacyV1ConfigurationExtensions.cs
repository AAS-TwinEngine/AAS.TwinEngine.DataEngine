using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Configuration.LegacyV1;

/// <summary>
/// Registers the V1 → V2 configuration adapters.
/// When V1 config is present, these adapters bind the old flat sections to old POCO classes,
/// then map them into the new V2 POCO shapes via <see cref="IConfigureOptions{T}"/>.
/// When V2 config is present, the adapters are registered but short-circuit (no-op).
/// </summary>
#pragma warning disable S1133 
[Obsolete("V1 configuration is deprecated and will be removed in next major release")]
public static class LegacyV1ConfigurationExtensions
{
    /// <summary>
    /// Adds IConfigureOptions adapters that read V1 flat config sections and populate V2 POCO classes.
    /// Must be called BEFORE <c>services.Configure&lt;GeneralConfig&gt;(…)</c> etc. so that the
    /// V2 section-bind (if present) overwrites the adapter-provided defaults.
    /// </summary>

    [Obsolete("V1 configuration is deprecated and will be removed in next major release")]
    public static IServiceCollection AddLegacyV1ConfigurationAdapters(this IServiceCollection services)
    {
        _ = services.AddSingleton<IConfigureOptions<GeneralConfig>, LegacyGeneralConfigAdapter>();
        _ = services.AddSingleton<IConfigureOptions<PluginsConfig>, LegacyPluginsConfigAdapter>();
        _ = services.AddSingleton<IConfigureOptions<TemplateManagementConfig>, LegacyTemplateManagementConfigAdapter>();

        return services;
    }
}

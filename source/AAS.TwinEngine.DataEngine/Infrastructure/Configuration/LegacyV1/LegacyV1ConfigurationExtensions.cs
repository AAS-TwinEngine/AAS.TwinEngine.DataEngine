using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Configuration.LegacyV1;

/// <summary>
/// Registers the V1 → V2 configuration adapters.
/// When V1 config is present, these adapters bind the old flat sections to old POCO classes,
/// then map them into the new V2 POCO shapes via <see cref="IConfigureOptions{T}"/>.
/// When V2 config is present, the adapters are registered but short-circuit (no-op).
/// </summary>
[Obsolete("Remove in v2.0.0 — V1 configuration support will be dropped.")]
public static class LegacyV1ConfigurationExtensions
{
    /// <summary>
    /// Adds IConfigureOptions adapters that read V1 flat config sections and populate V2 POCO classes.
    /// Must be called BEFORE <c>services.Configure&lt;GeneralConfig&gt;(…)</c> etc. so that the
    /// V2 section-bind (if present) overwrites the adapter-provided defaults.
    /// </summary>
    [Obsolete("Remove in v2.0.0 — V1 configuration support will be dropped.")]
    public static IServiceCollection AddLegacyV1ConfigurationAdapters(this IServiceCollection services)
    {
        _ = services.AddSingleton<IConfigureOptions<GeneralConfig>, LegacyGeneralConfigAdapter>();
        _ = services.AddSingleton<IConfigureOptions<PluginsConfig>, LegacyPluginsConfigAdapter>();
        _ = services.AddSingleton<IConfigureOptions<TemplateManagementConfig>, LegacyTemplateManagementConfigAdapter>();
        _ = services.AddSingleton<IConfigureOptions<RegistrySettingsConfig>, LegacyRegistrySettingsConfigAdapter>();

        return services;
    }
}

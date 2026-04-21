using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Configuration.LegacyV1;

/// <summary>
/// V1 configuration POCO for the "PluginConfig" section.
/// The constants have been moved to <see cref="HttpClientNames"/> and <see cref="ApiPaths"/>.
/// Only the Section, Plugins list, and Plugin child class remain for V1 legacy adapter deserialization.
/// </summary>
#pragma warning disable S1133
[Obsolete("V1 configuration is deprecated and will be removed in next major release Use HttpClientNames and ApiPaths instead.")]
public class PluginConfig
{
    public const string Section = "PluginConfig";

    // ── Forwarded constants (kept temporarily so legacy adapters compile) ──

    public const string HttpClientNamePrefix = HttpClientNames.PluginDataProviderPrefix;

    public const string HealthCheckHttpClientNamePrefix = HttpClientNames.PluginHealthCheckPrefix;

    public const string MetaData = ApiPaths.PluginMetadata;

    public required List<Plugin> Plugins { get; set; }
}

#pragma warning disable S1133
[Obsolete("V1 configuration is deprecated and will be removed in next major release Use ServiceInstance instead.")]
public class Plugin
{
    public required string PluginName { get; set; }

    public required Uri PluginUrl { get; set; }
}

using AAS.TwinEngine.DataEngine.Infrastructure.Configuration.LegacyV1.ConfigV1;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Config;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Configuration.LegacyV1;

/// <summary>
/// Reads V1 flat config sections and maps them into the V2 <see cref="PluginsConfig"/> shape.
/// </summary>
#pragma warning disable S1133 
[Obsolete("V1 configuration is deprecated and will be removed in next major release")]
public sealed class LegacyPluginsConfigAdapter(IConfiguration configuration) : IConfigureOptions<PluginsConfig>
{
    private readonly IConfiguration _configuration = configuration;

    public void Configure(PluginsConfig options) => MapToConfig(_configuration, options);

    /// <summary>
    /// Static entry point used during DI registration to apply V1 mapping without BuildServiceProvider().
    /// </summary>
    public static void MapToConfig(IConfiguration configuration, PluginsConfig options)
    {
        if (!LegacyConfigurationDetector.IsV1Configuration(configuration))
        {
            ApplyV1PluginInstanceOverrides(configuration, options);
            return;
        }

        // Semantics (V1: "Semantics") → split into Plugins + TemplateManagement
        var semantics = configuration.GetSection(Semantics.Section).Get<Semantics>();
        if (semantics != null)
        {
            options.SubmodelElementIndexContextPrefix = semantics.SubmodelElementIndexContextPrefix;
            options.MultiLanguageProperty.SemanticPostfixSeparator = semantics.MultiLanguageSemanticPostfixSeparator;
        }

        // MultiLanguageProperty (V1: "MultiLanguageProperty")
        var mlpSettings = configuration.GetSection(MultiLanguagePropertySettings.Section).Get<MultiLanguagePropertySettings>();
        if (mlpSettings?.DefaultLanguages != null)
        {
            options.MultiLanguageProperty = new PluginMultiLanguagePropertyConfig
            {
                DefaultLanguages = mlpSettings.DefaultLanguages,
                SemanticPostfixSeparator = options.MultiLanguageProperty.SemanticPostfixSeparator
            };
        }

        // Resilience → Retry (V1: "HttpRetryPolicyOptions:PluginDataProvider")
        var retryPolicy = configuration.GetSection($"{HttpRetryPolicyOptions.Section}:{HttpRetryPolicyOptions.PluginDataProvider}").Get<HttpRetryPolicyOptions>();
        if (retryPolicy != null)
        {
            options.ResiliencePolicies.Retry.MaxRetryAttempts = retryPolicy.MaxRetryAttempts;
            options.ResiliencePolicies.Retry.DelayInSeconds = retryPolicy.DelayInSeconds;
        }

        // Plugin instances (V1: "PluginConfig:Plugins") → Plugins:Instances with property renames
        ApplyV1PluginInstanceOverrides(configuration, options);
    }

    /// <summary>
    /// If the V1 <c>PluginConfig:Plugins</c> section contains values (e.g. from V1-style env vars),
    /// overrides <see cref="PluginsConfig.Instances"/> with the mapped V1 values.
    /// Called in both V1 and V2 modes so that legacy env vars work even when
    /// <c>appsettings.json</c> already ships V2 sections.
    /// </summary>
    public static void ApplyV1PluginInstanceOverrides(IConfiguration configuration, PluginsConfig options)
    {
        var pluginConfig = configuration.GetSection(PluginConfig.Section).Get<PluginConfig>();
        var headerForwarding = configuration.GetSection(HeaderForwardingOptions.Section).Get<HeaderForwardingOptions>();

        if (pluginConfig?.Plugins != null && pluginConfig.Plugins.Count > 0)
        {
            options.Instances = pluginConfig.Plugins.Select(plugin => new ServiceInstance
            {
                Name = plugin.PluginName,
                BaseUrl = plugin.PluginUrl,
                HeaderMappings = ResolvePluginHeaderMappings(headerForwarding, plugin.PluginName)
            }).ToList();
        }
    }

    private static IList<HeaderMappingRule> ResolvePluginHeaderMappings(HeaderForwardingOptions? forwarding, string pluginName)
    {
        if (forwarding?.HeaderMappings.Plugins == null)
        {
            return [];
        }

        return forwarding.HeaderMappings.Plugins.TryGetValue(pluginName, out var rules)
            ? rules
            : [];
    }
}

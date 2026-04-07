using AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Config;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Configuration.LegacyV1;

/// <summary>
/// Reads V1 flat config sections and maps them into the V2 <see cref="PluginsConfig"/> shape.
/// </summary>
[Obsolete("Remove in v2.0.0 — V1 configuration support will be dropped.")]
public sealed class LegacyPluginsConfigAdapter(IConfiguration configuration) : IConfigureOptions<PluginsConfig>
{
    private readonly IConfiguration _configuration = configuration;

    public void Configure(PluginsConfig options)
    {
        if (!LegacyConfigurationDetector.IsV1Configuration(_configuration))
        {
            return;
        }

        // Semantics (V1: "Semantics") → split into Plugins + TemplateManagement
        var semantics = _configuration.GetSection(Semantics.Section).Get<Semantics>();
        if (semantics != null)
        {
            options.SubmodelElementIndexContextPrefix = semantics.SubmodelElementIndexContextPrefix;
            options.MultiLanguageProperty.SemanticPostfixSeparator = semantics.MultiLanguageSemanticPostfixSeparator;
        }

        // MultiLanguageProperty (V1: "MultiLanguageProperty")
        var mlpSettings = _configuration.GetSection(MultiLanguagePropertySettings.Section).Get<MultiLanguagePropertySettings>();
        if (mlpSettings?.DefaultLanguages != null)
        {
            options.MultiLanguageProperty = new PluginMultiLanguagePropertyConfig
            {
                DefaultLanguages = mlpSettings.DefaultLanguages,
                SemanticPostfixSeparator = options.MultiLanguageProperty.SemanticPostfixSeparator
            };
        }

        // Resilience → Retry (V1: "HttpRetryPolicyOptions:PluginDataProvider")
        var retryPolicy = _configuration.GetSection($"{HttpRetryPolicyOptions.Section}:{HttpRetryPolicyOptions.PluginDataProvider}").Get<HttpRetryPolicyOptions>();
        if (retryPolicy != null)
        {
            options.ResiliencePolicies.Retry.MaxRetryAttempts = retryPolicy.MaxRetryAttempts;
            options.ResiliencePolicies.Retry.DelayInSeconds = retryPolicy.DelayInSeconds;
        }

        // Plugin instances (V1: "PluginConfig:Plugins") → Plugins:Instances with property renames
        var pluginConfig = _configuration.GetSection(PluginConfig.Section).Get<PluginConfig>();
        var headerForwarding = _configuration.GetSection(HeaderForwardingOptions.Section).Get<HeaderForwardingOptions>();

        if (pluginConfig?.Plugins != null)
        {
            options.Instances = pluginConfig.Plugins.Select(plugin => new PluginInstance
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

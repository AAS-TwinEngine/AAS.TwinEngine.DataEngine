using AAS.TwinEngine.DataEngine.Infrastructure.Configuration.LegacyV1.ConfigV1;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Configuration.LegacyV1;

/// <summary>
/// Reads V1 flat config sections and maps them into the V2 <see cref="TemplateManagementConfig"/> shape.
/// </summary>
#pragma warning disable S1133 
[Obsolete("V1 configuration is deprecated and will be removed in next major release")]
public sealed class LegacyTemplateManagementConfigAdapter(IConfiguration configuration) : IConfigureOptions<TemplateManagementConfig>
{
    public void Configure(TemplateManagementConfig options) => MapToConfig(configuration, options);

    /// <summary>
    /// Static entry point used during DI registration to apply V1 mapping without BuildServiceProvider().
    /// </summary>
    public static void MapToConfig(IConfiguration configuration, TemplateManagementConfig options)
    {
        if (!LegacyConfigurationDetector.IsV1Configuration(configuration))
        {
            ApplyV1Overrides(configuration, options);
            return;
        }

        // Semantics:InternalSemanticId → TemplateManagement:Semantics:InternalSemanticId
        var semantics = configuration.GetSection(Semantics.Section).Get<Semantics>();
        if (semantics != null)
        {
            options.Semantics = new TemplateSemanticsConfig
            {
                InternalSemanticId = semantics.InternalSemanticId
            };
        }

        // TemplateMappingRules (V1: top-level "TemplateMappingRules")
        var mappingRules = configuration.GetSection(TemplateMappingRules.Section).Get<TemplateMappingRules>();
        if (mappingRules != null)
        {
            RemapLegacyExtractionRules(configuration, mappingRules);
            options.TemplateMappingRules = mappingRules;
        }

        // Resilience → Retry (V1: "HttpRetryPolicyOptions:TemplateProvider")
        var retryPolicy = configuration.GetSection($"{HttpRetryPolicyOptions.Section}:{HttpRetryPolicyOptions.TemplateProvider}").Get<HttpRetryPolicyOptions>();
        if (retryPolicy != null)
        {
            options.ResiliencePolicies.Retry.MaxRetryAttempts = retryPolicy.MaxRetryAttempts;
            options.ResiliencePolicies.Retry.DelayInSeconds = retryPolicy.DelayInSeconds;
        }

        // AasEnvironment base URLs → service endpoints
        ApplyV1ServiceEndpointOverrides(configuration, options);
    }

    /// <summary>
    /// If V1-specific sections exist (e.g. from V1-style env vars), overrides the corresponding
    /// V2 values. Called in both V1 and V2 modes so that legacy env vars work even when
    /// <c>appsettings.json</c> already ships V2 sections.
    /// </summary>
    public static void ApplyV1Overrides(IConfiguration configuration, TemplateManagementConfig options)
    {
        // Top-level TemplateMappingRules (V1-only; in V2 it is nested under TemplateManagement)
        var mappingRules = configuration.GetSection(TemplateMappingRules.Section).Get<TemplateMappingRules>();
        if (mappingRules?.SubmodelTemplateMappings?.Count > 0
            || mappingRules?.ShellTemplateMappings?.Count > 0
            || mappingRules?.AasIdExtractionRules?.Count > 0)
        {
            RemapLegacyExtractionRules(configuration, mappingRules);
            options.TemplateMappingRules = mappingRules;
        }

        // Semantics (V1: top-level "Semantics")
        var semantics = configuration.GetSection(Semantics.Section).Get<Semantics>();
        if (semantics != null && !string.IsNullOrEmpty(semantics.InternalSemanticId))
        {
            options.Semantics = new TemplateSemanticsConfig
            {
                InternalSemanticId = semantics.InternalSemanticId
            };
        }

        // Resilience (V1: "HttpRetryPolicyOptions:TemplateProvider")
        var retryPolicy = configuration.GetSection($"{HttpRetryPolicyOptions.Section}:{HttpRetryPolicyOptions.TemplateProvider}").Get<HttpRetryPolicyOptions>();
        if (retryPolicy != null)
        {
            options.ResiliencePolicies.Retry.MaxRetryAttempts = retryPolicy.MaxRetryAttempts;
            options.ResiliencePolicies.Retry.DelayInSeconds = retryPolicy.DelayInSeconds;
        }

        // AasEnvironment → service endpoints
        ApplyV1ServiceEndpointOverrides(configuration, options);
    }

    private static void ApplyV1ServiceEndpointOverrides(IConfiguration configuration, TemplateManagementConfig options)
    {
        var aasEnv = configuration.GetSection(AasEnvironmentConfig.Section).Get<AasEnvironmentConfig>();
        if (aasEnv == null)
        {
            return;
        }

        var headerForwarding = configuration.GetSection(HeaderForwardingOptions.Section).Get<HeaderForwardingOptions>();

        if (aasEnv.AasEnvironmentRepositoryBaseUrl != null)
        {
            var repoHeaders = headerForwarding?.HeaderMappings.TemplateRepository ?? [];

            options.AasTemplateRepository = new ServiceInstance
            {
                Name = HttpClientNames.AasTemplateRepository,
                BaseUrl = aasEnv.AasEnvironmentRepositoryBaseUrl,
                HeaderMappings = repoHeaders
            };
            options.SubmodelTemplateRepository = new ServiceInstance
            {
                Name = HttpClientNames.SubmodelTemplateRepository,
                BaseUrl = aasEnv.AasEnvironmentRepositoryBaseUrl,
                HeaderMappings = repoHeaders
            };
            options.ConceptDescriptionTemplateRepository = new ServiceInstance
            {
                Name = HttpClientNames.ConceptDescriptorTemplateRepository,
                BaseUrl = aasEnv.AasEnvironmentRepositoryBaseUrl,
                HeaderMappings = repoHeaders
            };
        }

        if (aasEnv.AasRegistryBaseUrl != null)
        {
            options.AasTemplateRegistry = new ServiceInstance
            {
                Name = HttpClientNames.AasRegistry,
                BaseUrl = aasEnv.AasRegistryBaseUrl,
                HeaderMappings = headerForwarding?.HeaderMappings.TemplateRegistry ?? []
            };
        }

        if (aasEnv.SubModelRegistryBaseUrl != null)
        {
            options.SubmodelTemplateRegistry = new ServiceInstance
            {
                Name = HttpClientNames.SubmodelRegistry,
                BaseUrl = aasEnv.SubModelRegistryBaseUrl,
                HeaderMappings = []
            };
        }
    }

    /// <summary>
    /// V1 used { "Pattern": "Split", "Separator": "/", "Index": 6 }
    /// where Pattern held the strategy name and Separator held the actual delimiter.
    /// Detects this by checking for a "Separator" key in the raw config and remaps accordingly.
    /// </summary>
    private static void RemapLegacyExtractionRules(IConfiguration configuration, TemplateMappingRules rules)
    {
        var rulesSection = configuration.GetSection($"{TemplateMappingRules.Section}:AasIdExtractionRules");

        for (var i = 0; i < rules.AasIdExtractionRules.Count; i++)
        {
            var separator = rulesSection.GetSection(i.ToString())["Separator"];

            if (string.IsNullOrEmpty(separator))
            {
                continue;
            }

            var rule = rules.AasIdExtractionRules[i];
            // In old config, "Pattern" was the strategy name (e.g. "Split")
            if (Enum.TryParse<ExtractionStrategy>(rule.Pattern, ignoreCase: true, out var strategy))
            {
                rule.Strategy = strategy;
            }

            rule.Pattern = separator;
        }
    }
}

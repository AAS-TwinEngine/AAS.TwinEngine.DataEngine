using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.TemplateProvider.Config;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Configuration.LegacyV1;

/// <summary>
/// Reads V1 flat config sections and maps them into the V2 <see cref="TemplateManagementConfig"/> shape.
/// </summary>
#pragma warning disable S1133 
[Obsolete("V1 configuration is deprecated and will be removed in v2.0.0 version.")]
public sealed class LegacyTemplateManagementConfigAdapter(IConfiguration configuration) : IConfigureOptions<TemplateManagementConfig>
{
    private readonly IConfiguration _configuration = configuration;

    public void Configure(TemplateManagementConfig options)
    {
        if (!LegacyConfigurationDetector.IsV1Configuration(_configuration))
        {
            return;
        }

        // Semantics:InternalSemanticId → TemplateManagement:Semantics:InternalSemanticId
        var semantics = _configuration.GetSection(Semantics.Section).Get<Semantics>();
        if (semantics != null)
        {
            options.Semantics = new TemplateSemanticsConfig
            {
                InternalSemanticId = semantics.InternalSemanticId
            };
        }

        // TemplateMappingRules (V1: top-level "TemplateMappingRules")
        var mappingRules = _configuration.GetSection(TemplateMappingRules.Section).Get<TemplateMappingRules>();
        if (mappingRules != null)
        {
            RemapLegacyExtractionRules(mappingRules);
            options.TemplateMappingRules = mappingRules;
        }

        // Resilience → Retry (V1: "HttpRetryPolicyOptions:TemplateProvider")
        var retryPolicy = _configuration.GetSection($"{HttpRetryPolicyOptions.Section}:{HttpRetryPolicyOptions.TemplateProvider}").Get<HttpRetryPolicyOptions>();
        if (retryPolicy != null)
        {
            options.ResiliencePolicies.Retry.MaxRetryAttempts = retryPolicy.MaxRetryAttempts;
            options.ResiliencePolicies.Retry.DelayInSeconds = retryPolicy.DelayInSeconds;
        }

        // AasEnvironment base URLs → service endpoints
        var aasEnv = _configuration.GetSection(AasEnvironmentConfig.Section).Get<AasEnvironmentConfig>();

        // Header mappings from HeaderForwarding
        var headerForwarding = _configuration.GetSection(HeaderForwardingOptions.Section).Get<HeaderForwardingOptions>();

        if (aasEnv != null)
        {
            // V1 uses a single AasEnvironmentRepositoryBaseUrl for all template repositories.
            // Map it to the TemplateRepository shorthand so the normalizer propagates
            // the same URL and headers to Aas/Submodel/ConceptDescription template repositories.
            options.TemplateRepository = new ServiceEndpoint
            {
                Name = AasEnvironmentConfig.TemplateRepository,
                BaseUrl = aasEnv.AasEnvironmentRepositoryBaseUrl,
                HeaderMappings = headerForwarding?.HeaderMappings.TemplateRepository ?? []
            };

            options.AasTemplateRegistry = new ServiceEndpoint
            {
                Name = AasEnvironmentConfig.AasRegistry,
                BaseUrl = aasEnv.AasRegistryBaseUrl,
                HeaderMappings = headerForwarding?.HeaderMappings.TemplateRegistry ?? []
            };

            options.SubmodelTemplateRegistry = new ServiceEndpoint
            {
                Name = AasEnvironmentConfig.SubmodelRegistry,
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
    private void RemapLegacyExtractionRules(TemplateMappingRules rules)
    {
        var rulesSection = _configuration.GetSection($"{TemplateMappingRules.Section}:AasIdExtractionRules");

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

using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.TemplateProvider.Config;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Configuration.LegacyV1;

/// <summary>
/// Reads V1 flat config sections and maps them into the V2 <see cref="TemplateManagementConfig"/> shape.
/// </summary>
[Obsolete("Remove in v2.0.0 — V1 configuration support will be dropped.")]
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
            options.AasTemplateRepository = new ServiceEndpoint
            {
                Name = AasEnvironmentConfig.AasEnvironmentRepoHttpClientName,
                BaseUrl = aasEnv.AasEnvironmentRepositoryBaseUrl,
                HeaderMappings = headerForwarding?.HeaderMappings.TemplateRepository ?? []
            };

            options.AasTemplateRegistry = new ServiceEndpoint
            {
                Name = AasEnvironmentConfig.AasRegistryHttpClientName,
                BaseUrl = aasEnv.AasRegistryBaseUrl,
                HeaderMappings = headerForwarding?.HeaderMappings.TemplateRegistry ?? []
            };

            options.SubmodelTemplateRegistry = new ServiceEndpoint
            {
                Name = AasEnvironmentConfig.SubmodelRegistryHttpClientName,
                BaseUrl = aasEnv.SubModelRegistryBaseUrl,
                HeaderMappings = []
            };

            options.SubmodelTemplateRepository = new ServiceEndpoint
            {
                Name = "submodel-template-repository",
                BaseUrl = aasEnv.AasEnvironmentRepositoryBaseUrl,
                HeaderMappings = []
            };
        }
    }
}

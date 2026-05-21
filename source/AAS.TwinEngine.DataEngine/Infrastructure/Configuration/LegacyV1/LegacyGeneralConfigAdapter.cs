using AAS.TwinEngine.DataEngine.Infrastructure.Configuration.LegacyV1.ConfigV1;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Config;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Configuration.LegacyV1;

/// <summary>
/// Reads V1 flat config sections and maps them into the V2 <see cref="GeneralConfig"/> shape.
/// Registered as <see cref="IConfigureOptions{GeneralConfig}"/> so the Options system
/// merges these values before any consumer resolves <c>IOptions&lt;GeneralConfig&gt;</c>.
/// </summary>
#pragma warning disable S1133 
[Obsolete("V1 configuration is deprecated and will be removed in next major release")]
public sealed class LegacyGeneralConfigAdapter(IConfiguration configuration) : IConfigureOptions<GeneralConfig>
{
    private readonly IConfiguration _configuration = configuration;

    public void Configure(GeneralConfig options)
    {
        if (!LegacyConfigurationDetector.IsV1Configuration(_configuration))
        {
            ApplyV1Overrides(_configuration, options);
            return;
        }

        // ApiConfiguration (V1: top-level "ApiConfiguration")
        // Use Bind() to avoid importing Api.Configuration namespace (clean architecture rule).
        _configuration.GetSection("ApiConfiguration").Bind(options.ApiConfiguration);

        // AasEnvironment URLs (V1: top-level "AasEnvironment") → flat GeneralConfig properties
        var aasEnv = _configuration.GetSection(AasEnvironmentConfig.Section).Get<AasEnvironmentConfig>();
        if (aasEnv != null)
        {
            options.CustomerDomainUrl = aasEnv.CustomerDomainUrl;
            options.DataEngineRepositoryBaseUrl = aasEnv.DataEngineRepositoryBaseUrl;
        }

        // HeaderSanitization (V1: "HeaderForwarding:HeaderSanitization")
        var sanitization = _configuration.GetSection($"{HeaderForwardingOptions.Section}:HeaderSanitization").Get<HeaderSanitizationOptions>();
        if (sanitization != null)
        {
            options.HeaderSanitization = sanitization;
        }

        // AllowedHosts (V1: top-level "AllowedHosts")
        var allowedHosts = _configuration["AllowedHosts"];
        if (!string.IsNullOrEmpty(allowedHosts))
        {
            options.AllowedHosts = allowedHosts;
        }

        // OpenTelemetry (V1: top-level "OpenTelemetry")
        var otel = _configuration.GetSection(OpenTelemetrySettings.Section).Get<OpenTelemetrySettings>();
        if (otel != null)
        {
            options.OpenTelemetry = otel;
        }
    }

    /// <summary>
    /// If V1-specific sections exist (e.g. from V1-style env vars), overrides the corresponding
    /// V2 values. Called in both V1 and V2 modes so that legacy env vars work even when
    /// <c>appsettings.json</c> already ships V2 sections.
    /// </summary>
    public static void ApplyV1Overrides(IConfiguration configuration, GeneralConfig options)
    {
        // AasEnvironment (V1-only top-level section)
        var aasEnv = configuration.GetSection(AasEnvironmentConfig.Section).Get<AasEnvironmentConfig>();
        if (aasEnv != null)
        {
            if (aasEnv.CustomerDomainUrl != null)
            {
                options.CustomerDomainUrl = aasEnv.CustomerDomainUrl;
            }

            if (aasEnv.DataEngineRepositoryBaseUrl != null)
            {
                options.DataEngineRepositoryBaseUrl = aasEnv.DataEngineRepositoryBaseUrl;
            }
        }
    }
}

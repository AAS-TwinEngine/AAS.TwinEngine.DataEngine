using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Config;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Configuration.LegacyV1;

/// <summary>
/// Reads V1 flat config sections and maps them into the V2 <see cref="GeneralConfig"/> shape.
/// Registered as <see cref="IConfigureOptions{GeneralConfig}"/> so the Options system
/// merges these values before any consumer resolves <c>IOptions&lt;GeneralConfig&gt;</c>.
/// </summary>
[Obsolete("Remove in v2.0.0 — V1 configuration support will be dropped.")]
public sealed class LegacyGeneralConfigAdapter(IConfiguration configuration) : IConfigureOptions<GeneralConfig>
{
    private readonly IConfiguration _configuration = configuration;

    public void Configure(GeneralConfig options)
    {
        if (!LegacyConfigurationDetector.IsV1Configuration(_configuration))
        {
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
}

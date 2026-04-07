using AAS.TwinEngine.DataEngine.Api.Configuration;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Config;

namespace AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

/// <summary>
/// V2 config — binds to the "General" section.
/// Contains cross-cutting / infrastructure concerns.
/// </summary>
public class GeneralConfig
{
    public const string Section = "General";

    public ApiConfiguration ApiConfiguration { get; set; } = new();
    public HeaderSanitizationOptions HeaderSanitization { get; set; } = new();
    public string AllowedHosts { get; set; } = "*";
    public OpenTelemetrySettings OpenTelemetry { get; set; } = new();

    /// <summary>
    /// Domain URL of the customer environment (V2: direct property; V1: was AasEnvironment:CustomerDomainUrl).
    /// </summary>
    public Uri CustomerDomainUrl { get; set; } = null!;

    /// <summary>
    /// Base URL of the DataEngine's own repository.
    /// V1: populated by legacy adapter from AasEnvironment:DataEngineRepositoryBaseUrl.
    /// V2: null — the URL is derived from the incoming HTTP request at runtime.
    /// </summary>
    public Uri? DataEngineRepositoryBaseUrl { get; set; }

    // Note: Serilog reads directly from IConfiguration, not via POCO
}

namespace AAS.TwinEngine.DataEngine.Infrastructure.Configuration.LegacyV1;

/// <summary>
/// V1 configuration POCO for the "AasEnvironment" section.
/// The constants have been moved to <see cref="HttpClientNames"/> and <see cref="ApiPaths"/>.
/// Only the URI properties and Section remain for V1 legacy adapter deserialization.
/// </summary>
[Obsolete("V1 configuration is deprecated and will be removed in v2.0.0 version. Use HttpClientNames and ApiPaths instead.")]
public class AasEnvironmentConfig
{
    public const string Section = "AasEnvironment";

    public Uri DataEngineRepositoryBaseUrl { get; set; } = null!;

    public Uri? AasEnvironmentRepositoryBaseUrl { get; set; } = null!;

    public Uri? AasRegistryBaseUrl { get; set; } = null!;

    public Uri? SubModelRegistryBaseUrl { get; set; } = null!;

    public Uri CustomerDomainUrl { get; set; } = null!;
}

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Config;

public class AasEnvironmentConfig
{
    public const string Section = "AasEnvironment";

    public const string AasEnvironmentRepoHttpClientName = "template-repository";
    public const string AasRegistryHttpClientName = "aas-registry";
    public const string SubmodelRegistryHttpClientName = "submodel-registry";
    public const string AasEnvironmentRepoHealthCheckHttpClientName = "template-repository-healthcheck";
    public const string AasRegistryHealthCheckHttpClientName = "aas-registry-healthcheck";
    public const string SubmodelRegistryHealthCheckHttpClientName = "submodel-registry-healthcheck";

    // Path constants (no longer configurable — fixed API contracts)
    public const string SubModelRepositoryPath = "submodels";
    public const string AasRegistryPath = "shell-descriptors";
    public const string SubModelRegistryPath = "submodel-descriptors";
    public const string AasRepositoryPath = "shells";
    public const string SubmodelRefPath = "submodel-refs";

    public const string ConceptDescriptionPath = "concept-descriptions";

    // V1-bindable URI properties (used only by LegacyV1 adapter)
    public Uri DataEngineRepositoryBaseUrl { get; set; } = null!;

    public Uri? AasEnvironmentRepositoryBaseUrl { get; set; } = null!;

    public Uri? AasRegistryBaseUrl { get; set; } = null!;

    public Uri? SubModelRegistryBaseUrl { get; set; } = null!;
    public Uri CustomerDomainUrl { get; set; } = null!;
}

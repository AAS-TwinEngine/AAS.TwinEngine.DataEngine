namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Config;

public class AasEnvironmentConfig
{
    public const string Section = "AasEnvironment";

    public const string TemplateRepository = "template-repository";
    public const string AasRegistry = "aas-registry";
    public const string SubmodelRegistry = "submodel-registry";
    public const string SubmodelTemplateRepository = "submodel-template-repository";
    public const string AasTemplateRepository = "aas-template-repository";
    public const string ConceptDescriptorTemplateRepository = "concept-descriptor-template-repository";

    public const string TemplateRepositoryHealthCheck = TemplateRepository + "-healthcheck";
    public const string AasRegistryHealthCheck = AasRegistry + "-healthcheck";
    public const string SubmodelRegistryHealthCheck = SubmodelRegistry + "-healthcheck";
    public const string SubmodelTemplateRepositoryHealthCheck = SubmodelTemplateRepository + "-healthcheck";
    public const string AasTemplateRepositoryHealthCheck = AasTemplateRepository + "-healthcheck";
    public const string ConceptDescriptorTemplateRepositoryHealthCheck = ConceptDescriptorTemplateRepository + "-healthcheck";

    // Backward-compatible aliases used by existing code (TemplateProvider, health checks, etc.)
    public const string AasEnvironmentRepoHttpClientName = AasTemplateRepository;
    public const string AasRegistryHttpClientName = AasRegistry;
    public const string SubmodelRegistryHttpClientName = SubmodelRegistry;
    public const string AasEnvironmentRepoHealthCheckHttpClientName = AasTemplateRepositoryHealthCheck;
    public const string AasRegistryHealthCheckHttpClientName = AasRegistryHealthCheck;
    public const string SubmodelRegistryHealthCheckHttpClientName = SubmodelRegistryHealthCheck;

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

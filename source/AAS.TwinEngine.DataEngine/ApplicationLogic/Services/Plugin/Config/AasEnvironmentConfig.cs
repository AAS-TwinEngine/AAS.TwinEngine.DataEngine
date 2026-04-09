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

    private const string HealthCheckSuffix = "-healthcheck";

    public static string GetHealthCheckName(string clientName) => $"{clientName}{HealthCheckSuffix}";

    public static string TemplateRepositoryHealthCheck => GetHealthCheckName(TemplateRepository);
    public static string AasRegistryHealthCheck => GetHealthCheckName(AasRegistry);
    public static string SubmodelRegistryHealthCheck => GetHealthCheckName(SubmodelRegistry);
    public static string SubmodelTemplateRepositoryHealthCheck => GetHealthCheckName(SubmodelTemplateRepository);
    public static string AasTemplateRepositoryHealthCheck => GetHealthCheckName(AasTemplateRepository);
    public static string ConceptDescriptorTemplateRepositoryHealthCheck => GetHealthCheckName(ConceptDescriptorTemplateRepository);

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

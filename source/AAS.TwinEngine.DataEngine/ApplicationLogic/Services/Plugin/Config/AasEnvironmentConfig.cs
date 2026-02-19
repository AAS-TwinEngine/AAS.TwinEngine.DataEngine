namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Config;

public class AasEnvironmentConfig
{
    public const string Section = "AasEnvironment";

    public const string AasEnvironmentRepoHttpClientName = "template-repository";

    public const string AasRegistryHttpClientName = "aas-registry";

    public const string SubmodelRegistryHttpClientName = "submodel-registry";

    public Uri DataEngineRepositoryBaseUrl { get; set; } = null!;

    public Uri? AasEnvironmentRepositoryBaseUrl { get; set; } = null!;

    public Uri? AasRegistryBaseUrl { get; set; } = null!;

    public string SubModelRepositoryPath { get; set; } = "submodels";

    public string AasRegistryPath { get; set; } = "shell-descriptors";

    public Uri? SubModelRegistryBaseUrl { get; set; } = null!;

    public string SubModelRegistryPath { get; set; } = "submodel-descriptors";

    public string AasRepositoryPath { get; set; } = "shells";

    public string SubmodelRefPath { get; set; } = "submodel-refs";

    public string ConceptDescriptionPath { get; set; } = "concept-descriptions";

    public Uri CustomerDomainUrl { get; set; } = null!;
}

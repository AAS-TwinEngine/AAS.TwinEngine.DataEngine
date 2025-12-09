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

    public string SubModelRepositoryPath { get; set; } = null!;

    public string AasRegistryPath { get; set; } = null!;

    public Uri? SubModelRegistryBaseUrl { get; set; } = null!;

    public string SubModelRegistryPath { get; set; } = null!;

    public string AasRepositoryPath { get; set; } = null!;

    public string SubmodelRefPath { get; set; } = null!;

    public string ConceptDescriptionPath { get; set; } = null!;

    public Uri CustomerDomainUrl { get; set; } = null!;
}

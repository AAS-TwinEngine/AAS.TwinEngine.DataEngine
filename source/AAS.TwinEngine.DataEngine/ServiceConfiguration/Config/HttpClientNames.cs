namespace AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

/// <summary>
/// Named HttpClient identifiers used for DI registration, HttpClientFactory resolution,
/// and health check client creation. Centralises all client-name constants so they are
/// independent of legacy configuration classes.
/// </summary>
public static class HttpClientNames
{
    public const string AasRegistry = "aas-registry";
    public const string SubmodelRegistry = "submodel-registry";
    public const string SubmodelTemplateRepository = "submodel-template-repository";
    public const string AasTemplateRepository = "aas-template-repository";
    public const string ConceptDescriptorTemplateRepository = "concept-descriptor-template-repository";

    // ── Plugin client name prefixes ──

    public const string PluginDataProviderPrefix = "plugin-data-provider";
    public const string PluginHealthCheckPrefix = "plugin-healthcheck";

    private const string HealthCheckSuffix = "-healthcheck";

    public static string GetHealthCheckName(string clientName) => $"{clientName}{HealthCheckSuffix}";

    public static string AasRegistryHealthCheck => GetHealthCheckName(AasRegistry);
    public static string SubmodelRegistryHealthCheck => GetHealthCheckName(SubmodelRegistry);
    public static string SubmodelTemplateRepositoryHealthCheck => GetHealthCheckName(SubmodelTemplateRepository);
    public static string AasTemplateRepositoryHealthCheck => GetHealthCheckName(AasTemplateRepository);
    public static string ConceptDescriptorTemplateRepositoryHealthCheck => GetHealthCheckName(ConceptDescriptorTemplateRepository);
}

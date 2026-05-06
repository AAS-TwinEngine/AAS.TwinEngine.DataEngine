using AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Config;

namespace AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

/// <summary>
/// V2 config — binds to the "TemplateManagement" section.
/// Contains template repositories, registries, mapping rules, and semantics.
/// </summary>
public class TemplateManagementConfig
{
    public const string Section = "TemplateManagement";

    public ResiliencePoliciesConfig ResiliencePolicies { get; set; } = new();
    public TemplateMappingRules TemplateMappingRules { get; set; } = new();
    public TemplateSemanticsConfig Semantics { get; set; } = new();

    public ServiceInstance AasTemplateRepository { get; set; } = new();
    public ServiceInstance SubmodelTemplateRepository { get; set; } = new();
    public ServiceInstance ConceptDescriptionTemplateRepository { get; set; } = new();
    public ServiceInstance AasTemplateRegistry { get; set; } = new();
    public ServiceInstance SubmodelTemplateRegistry { get; set; } = new();
}

/// <summary>
/// Semantics configuration for template management (InternalSemanticId only).
/// </summary>
public class TemplateSemanticsConfig
{
    public string InternalSemanticId { get; set; } = "InternalSemanticId";
}

/// <summary>
/// A named service instance with a base URL and co-located header mappings.
/// Used for both plugin instances and template service endpoints.
/// </summary>
public class ServiceInstance
{
    public string Name { get; set; } = string.Empty;
    public Uri? BaseUrl { get; set; }
    public IList<HeaderMappingRule> HeaderMappings { get; init; } = [];
    public string HealthEndpoint { get; set; } = string.Empty;
}

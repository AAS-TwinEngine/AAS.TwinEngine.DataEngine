using AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Config;

namespace AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

/// <summary>
/// V2 config — binds to the "Plugins" section.
/// Contains all plugin-related configuration.
/// </summary>
public class PluginsConfig
{
    public const string Section = "Plugins";

    public string SubmodelElementIndexContextPrefix { get; set; } = "_aastwinengineindex_";
    public PluginMultiLanguagePropertyConfig MultiLanguageProperty { get; set; } = new();
    public ResiliencePoliciesConfig ResiliencePolicies { get; set; } = new();
    public IList<PluginInstance> Instances { get; set; } = [];
}

/// <summary>
/// Multi-language property settings co-located with plugins.
/// Combines the old MultiLanguagePropertySettings.DefaultLanguages with
/// the old Semantics.MultiLanguageSemanticPostfixSeparator (renamed).
/// </summary>
public class PluginMultiLanguagePropertyConfig
{
    public IList<string>? DefaultLanguages { get; init; }
    public string SemanticPostfixSeparator { get; set; } = "_";
}

/// <summary>
/// A single plugin instance. Replaces old Plugin class with renamed properties
/// and co-located header mappings.
/// </summary>
public class PluginInstance
{
    public required string Name { get; set; }
    public required Uri BaseUrl { get; set; }
    public IList<HeaderMappingRule> HeaderMappings { get; init; } = [];
}

/// <summary>
/// Resilience policies shared within a domain (Plugins or TemplateManagement).
/// </summary>
public class ResiliencePoliciesConfig
{
    public RetryConfig Retry { get; set; } = new();
}

public class RetryConfig
{
    public int MaxRetryAttempts { get; set; }
    public int DelayInSeconds { get; set; }
    public double BackoffMultiplier { get; set; } = 2.0;
}

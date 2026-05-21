namespace AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

/// <summary>
/// Fixed API path segments used to build request URIs for AAS-specification endpoints.
/// These are protocol constants, not user-configurable values.
/// </summary>
public static class ApiPaths
{
    public const string Submodels = "submodels";
    public const string ShellDescriptors = "shell-descriptors";
    public const string SubmodelDescriptors = "submodel-descriptors";
    public const string Shells = "shells";
    public const string SubmodelRefs = "submodel-refs";
    public const string ConceptDescriptions = "concept-descriptions";

    // ── Plugin API paths ──

    public const string PluginMetadata = "metadata";
}

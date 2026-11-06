namespace AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

/// <summary>
/// Named HttpClient identifiers used for DI registration and factory resolution.
/// Names are stable strings so they can be referenced from configuration and logs.
/// </summary>
public static class HttpClientNames
{
    // ── Sources ──
    public const string SourceShellDescriptors = "source-shell-descriptors";
    public const string SourceSubmodelDescriptors = "source-submodel-descriptors";
    public const string SourceShells = "source-shells";
    public const string SourceSubmodels = "source-submodels";
    public const string SourceConceptDescriptions = "source-concept-descriptions";

    // ── Targets ──
    public const string TargetShellDescriptors = "target-shell-descriptors";
    public const string TargetSubmodelDescriptors = "target-submodel-descriptors";
    public const string TargetShells = "target-shells";
    public const string TargetSubmodels = "target-submodels";
    public const string TargetConceptDescriptions = "target-concept-descriptions";

    // ── OAuth token acquisition (no auth handler on this one) ──
    public const string OAuthToken = "oauth-token";
}

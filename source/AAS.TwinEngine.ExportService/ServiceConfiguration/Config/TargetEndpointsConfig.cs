namespace AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

/// <summary>
/// Endpoints the exporter writes to (target systems). Each target is enabled by
/// the corresponding source endpoint's Enabled setting.
/// </summary>
public sealed class TargetEndpointsConfig
{
    public bool OmitNullProperties { get; set; }

    public EndpointConfig ShellDescriptors { get; set; } = new();

    public EndpointConfig SubmodelDescriptors { get; set; } = new();

    public EndpointConfig Shells { get; set; } = new();

    public EndpointConfig Submodels { get; set; } = new();

    public EndpointConfig ConceptDescriptions { get; set; } = new();
}

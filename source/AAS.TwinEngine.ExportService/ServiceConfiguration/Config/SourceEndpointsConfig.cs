namespace AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

/// <summary>
/// Endpoints the exporter reads from (source systems).
/// </summary>
public sealed class SourceEndpointsConfig
{
    public EndpointConfig ShellDescriptors { get; set; } = new();

    public EndpointConfig SubmodelDescriptors { get; set; } = new();

    public EndpointConfig Shells { get; set; } = new();

    public EndpointConfig Submodels { get; set; } = new();

    public EndpointConfig ConceptDescriptions { get; set; } = new();
}

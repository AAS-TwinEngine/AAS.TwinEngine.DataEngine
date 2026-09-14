namespace AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

/// <summary>
/// Endpoints the exporter writes to (target systems). Same shape as sources so both sides
/// stay symmetric and easy to reason about in configuration.
/// </summary>
public sealed class TargetEndpointsConfig
{
    public EndpointConfig ShellDescriptors { get; set; } = new();

    public EndpointConfig SubmodelDescriptors { get; set; } = new();

    public EndpointConfig Shells { get; set; } = new();

    public EndpointConfig Submodels { get; set; } = new();

    public EndpointConfig ConceptDescriptions { get; set; } = new();
}

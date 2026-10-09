using AAS.TwinEngine.ExportService.DomainModel;
using AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

namespace AAS.TwinEngine.ExportService.Infrastructure.Http.Clients;

/// <summary>
/// Maps <see cref="EntityKind"/> to the named HttpClient and configured endpoint used to
/// read from source / write to target. Keeps the wiring in one small file so client code
/// stays readable.
/// </summary>
internal static class EndpointResolver
{
    public static string SourceClientName(EntityKind kind) => kind switch
    {
        EntityKind.ConceptDescription => HttpClientNames.SourceConceptDescriptions,
        EntityKind.Submodel => HttpClientNames.SourceSubmodels,
        EntityKind.SubmodelDescriptor => HttpClientNames.SourceSubmodelDescriptors,
        EntityKind.Shell => HttpClientNames.SourceShells,
        EntityKind.ShellDescriptor => HttpClientNames.SourceShellDescriptors,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    public static string TargetClientName(EntityKind kind) => kind switch
    {
        EntityKind.ConceptDescription => HttpClientNames.TargetConceptDescriptions,
        EntityKind.Submodel => HttpClientNames.TargetSubmodels,
        EntityKind.SubmodelDescriptor => HttpClientNames.TargetSubmodelDescriptors,
        EntityKind.Shell => HttpClientNames.TargetShells,
        EntityKind.ShellDescriptor => HttpClientNames.TargetShellDescriptors,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    public static EndpointConfig SourceEndpoint(EntityKind kind, SourceEndpointsConfig sources) => kind switch
    {
        EntityKind.ConceptDescription => sources.ConceptDescriptions,
        EntityKind.Submodel => sources.Submodels,
        EntityKind.SubmodelDescriptor => sources.SubmodelDescriptors,
        EntityKind.Shell => sources.Shells,
        EntityKind.ShellDescriptor => sources.ShellDescriptors,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    public static EndpointConfig TargetEndpoint(EntityKind kind, TargetEndpointsConfig targets) => kind switch
    {
        EntityKind.ConceptDescription => targets.ConceptDescriptions,
        EntityKind.Submodel => targets.Submodels,
        EntityKind.SubmodelDescriptor => targets.SubmodelDescriptors,
        EntityKind.Shell => targets.Shells,
        EntityKind.ShellDescriptor => targets.ShellDescriptors,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}

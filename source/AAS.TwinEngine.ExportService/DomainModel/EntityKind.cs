namespace AAS.TwinEngine.ExportService.DomainModel;

/// <summary>
/// The types of AAS entities the exporter can synchronize.
/// Order of the enum values reflects the required export order (dependency-first).
/// </summary>
public enum EntityKind
{
    ConceptDescription = 1,
    Submodel = 2,
    SubmodelDescriptor = 3,
    Shell = 4,
    ShellDescriptor = 5
}

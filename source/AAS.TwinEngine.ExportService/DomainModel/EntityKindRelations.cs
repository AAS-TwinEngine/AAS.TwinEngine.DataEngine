namespace AAS.TwinEngine.ExportService.DomainModel;

/// <summary>
/// Relations between entity kinds that follow from the AAS specification.
/// </summary>
public static class EntityKindRelations
{
    /// <summary>
    /// Returns the kind of the entity that <paramref name="kind"/> describes, or <c>null</c>
    /// when the kind is not a registry descriptor. A descriptor carries the same identifier as
    /// the shell or submodel it describes, so ownership of that entity also identifies the
    /// descriptor belonging to it.
    /// </summary>
    public static EntityKind? DescribedEntityKind(this EntityKind kind) => kind switch
    {
        EntityKind.ShellDescriptor => EntityKind.Shell,
        EntityKind.SubmodelDescriptor => EntityKind.Submodel,
        _ => null
    };
}

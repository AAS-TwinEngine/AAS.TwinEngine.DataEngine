namespace AAS.TwinEngine.ExportService.DomainModel;

/// <summary>
/// Aggregate outcome of exporting a single entity kind (one phase of a run).
/// </summary>
public sealed record PhaseResult(
    EntityKind Kind,
    int Created,
    int Updated,
    int Deleted,
    int Skipped,
    int Failed)
{
    public bool HasFailures => Failed > 0;

    public static PhaseResult Empty(EntityKind kind) => new(kind, 0, 0, 0, 0, 0);
}

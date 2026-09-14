namespace AAS.TwinEngine.ExportService.DomainModel;

/// <summary>
/// State record maintained by the exporter: proof that this specific entity is managed
/// by <c>this</c> service and therefore eligible for update/delete against the target.
/// </summary>
public sealed record ExportedEntity(
    EntityKind Kind,
    string Identifier,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastSyncedAt);

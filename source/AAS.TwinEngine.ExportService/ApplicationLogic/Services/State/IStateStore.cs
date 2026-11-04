using AAS.TwinEngine.ExportService.DomainModel;

namespace AAS.TwinEngine.ExportService.ApplicationLogic.Services.State;

/// <summary>
/// Persistence for the exporter's ownership record: which entities <em>this</em>
/// service has created in the target.
/// </summary>
public interface IStateStore
{
    Task EnsureSchemaAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ExportedEntity>> LoadAsync(EntityKind kind, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the ownership record for a single entity, or <c>null</c> when this service does
    /// not manage it.
    /// </summary>
    Task<ExportedEntity?> GetAsync(EntityKind kind, string identifier, CancellationToken cancellationToken);

    Task UpsertAsync(ExportedEntity entity, CancellationToken cancellationToken);

    Task DeleteAsync(EntityKind kind, string identifier, CancellationToken cancellationToken);
}

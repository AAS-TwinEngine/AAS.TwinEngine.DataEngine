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

    Task UpsertAsync(ExportedEntity entity, CancellationToken cancellationToken);

    Task DeleteAsync(EntityKind kind, string identifier, CancellationToken cancellationToken);
}

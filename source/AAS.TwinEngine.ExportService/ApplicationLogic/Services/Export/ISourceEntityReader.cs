using AAS.TwinEngine.ExportService.DomainModel;

namespace AAS.TwinEngine.ExportService.ApplicationLogic.Services.Export;

/// <summary>
/// Reads all entities of a given kind from the configured source system (DataEngine or
/// Template Repository for Concept Descriptions).
/// </summary>
public interface ISourceEntityReader
{
    /// <summary>
    /// Returns every entity of <paramref name="kind"/>. If the endpoint is disabled or
    /// unreachable, throws <see cref="Exceptions.SourceUnavailableException"/>.
    /// </summary>
    Task<IReadOnlyList<SourceEntity>> ReadAllAsync(EntityKind kind, CancellationToken cancellationToken);

    /// <summary>
    /// Fetches a single entity of <paramref name="kind"/> by identifier. Used to verify whether a
    /// deletion candidate still exists at the source before removing it from the target.
    /// Returns the entity when the source reports it as present, or <c>null</c> when the source
    /// reports it as not found (HTTP 404 — confirmed deleted). Throws
    /// <see cref="Exceptions.SourceUnavailableException"/> when existence cannot be determined
    /// (transient/server/network error) so the caller can safely skip the deletion and retry later.
    /// </summary>
    Task<SourceEntity?> GetByIdAsync(EntityKind kind, string identifier, CancellationToken cancellationToken);
}

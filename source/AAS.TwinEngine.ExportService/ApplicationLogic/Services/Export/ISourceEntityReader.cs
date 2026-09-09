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
}

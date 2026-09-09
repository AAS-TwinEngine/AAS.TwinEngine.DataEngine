using AAS.TwinEngine.ExportService.DomainModel;

namespace AAS.TwinEngine.ExportService.ApplicationLogic.Services.Export;

/// <summary>
/// Writes a single entity to the target system for a given kind.
/// Implementations translate the semantic operation into the correct HTTP verb and route.
/// </summary>
public interface ITargetEntityWriter
{
    Task CreateAsync(EntityKind kind, SourceEntity entity, CancellationToken cancellationToken);

    Task UpdateAsync(EntityKind kind, SourceEntity entity, CancellationToken cancellationToken);

    Task DeleteAsync(EntityKind kind, string identifier, CancellationToken cancellationToken);
}

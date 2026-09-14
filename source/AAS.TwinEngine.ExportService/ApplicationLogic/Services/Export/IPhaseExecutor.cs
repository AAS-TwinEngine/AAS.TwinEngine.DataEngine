using AAS.TwinEngine.ExportService.DomainModel;

namespace AAS.TwinEngine.ExportService.ApplicationLogic.Services.Export;

/// <summary>
/// Executes one phase (one entity kind): read from source, load state, decide operations,
/// apply them to the target, and update state. Per-entity failures are counted and logged
/// but do not abort the phase. A total source-read failure aborts the phase (the runner
/// then cancels subsequent phases to preserve referential integrity).
/// </summary>
public interface IPhaseExecutor
{
    Task<PhaseResult> ExecuteAsync(EntityKind kind, CancellationToken cancellationToken);
}

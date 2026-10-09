using AAS.TwinEngine.ExportService.DomainModel;

namespace AAS.TwinEngine.ExportService.ApplicationLogic.Services.Export;

/// <summary>
/// Orchestrates a full export cycle. Executes the five phases in referential order
/// (Concept Descriptions → Submodels → Submodel Descriptors → Shells → Shell Descriptors).
/// If a phase aborts because its source is unreachable, subsequent phases are cancelled.
/// </summary>
public interface IExportRunner
{
    Task<ExportRunResult> RunAsync(CancellationToken cancellationToken);
}

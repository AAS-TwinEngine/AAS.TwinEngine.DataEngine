using AAS.TwinEngine.ExportService.ApplicationLogic.Exceptions;
using AAS.TwinEngine.ExportService.ApplicationLogic.Observability;
using AAS.TwinEngine.ExportService.DomainModel;
using AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.ExportService.ApplicationLogic.Services.Export;

public sealed class ExportRunner : IExportRunner
{
    private static readonly EntityKind[] PhaseOrder =
    {
        EntityKind.ConceptDescription,
        EntityKind.Submodel,
        EntityKind.SubmodelDescriptor,
        EntityKind.Shell,
        EntityKind.ShellDescriptor
    };

    private readonly IPhaseExecutor _phaseExecutor;
    private readonly IOptions<ExportServiceConfig> _config;
    private readonly ILogger<ExportRunner> _logger;

    public ExportRunner(
        IPhaseExecutor phaseExecutor,
        IOptions<ExportServiceConfig> config,
        ILogger<ExportRunner> logger)
    {
        _phaseExecutor = phaseExecutor;
        _config = config;
        _logger = logger;
    }

    public async Task<ExportRunResult> RunAsync(CancellationToken cancellationToken)
    {
        using var runSpan = ExportServiceTracing.StartSpan(ExportServiceTracing.Spans.ExportRun);

        var startedAt = DateTimeOffset.UtcNow;
        _logger.LogInformation("Export run started at {StartedAt:O}", startedAt);

        var phases = new Dictionary<EntityKind, PhaseResult>(PhaseOrder.Length);
        var enabledPhases = new List<EntityKind>(PhaseOrder.Length);
        var status = RunStatus.Success;

        foreach (var kind in PhaseOrder)
        {
            if (!IsPhaseEnabled(kind))
            {
                _logger.LogInformation("Phase {EntityKind} is disabled by configuration. Skipping.", kind);
                continue;
            }

            enabledPhases.Add(kind);
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var result = await _phaseExecutor.ExecuteAsync(kind, cancellationToken).ConfigureAwait(false);
                phases.Add(kind, result);

                if (result.HasFailures && status == RunStatus.Success)
                {
                    status = RunStatus.PartialFailure;
                }
            }
            catch (SourceUnavailableException ex)
            {
                _logger.LogCritical(
                    ex,
                    "Phase {EntityKind} aborted: source unavailable. Cancelling remaining phases to preserve referential integrity.",
                    kind);
                status = RunStatus.Aborted;
                break;
            }
            catch (OperationCanceledException ex)
            {
                throw new OperationCanceledException(
                    $"Export run was cancelled during phase {kind}.", ex, ex.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Unexpected failure in phase {EntityKind}. Cancelling remaining phases.", kind);
                status = RunStatus.Aborted;
                break;
            }
        }

        if (status != RunStatus.Aborted)
        {
            foreach (var kind in enabledPhases.AsEnumerable().Reverse())
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var deletionResult = await _phaseExecutor.ExecuteDeletionsAsync(kind, cancellationToken).ConfigureAwait(false);
                    phases[kind] = Merge(phases[kind], deletionResult);

                    if (deletionResult.HasFailures && status == RunStatus.Success)
                    {
                        status = RunStatus.PartialFailure;
                    }
                }
                catch (SourceUnavailableException ex)
                {
                    _logger.LogCritical(
                        ex,
                        "Deletion phase {EntityKind} aborted: source unavailable. Cancelling remaining deletions.",
                        kind);
                    status = RunStatus.Aborted;
                    break;
                }
                catch (OperationCanceledException ex)
                {
                    throw new OperationCanceledException(
                        $"Export run was cancelled during deletion phase {kind}.", ex, ex.CancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Unexpected failure in deletion phase {EntityKind}. Cancelling remaining deletions.", kind);
                    status = RunStatus.Aborted;
                    break;
                }
            }
        }

        var finishedAt = DateTimeOffset.UtcNow;
        _logger.LogInformation(
            "Export run finished at {FinishedAt:O} with status {Status}. Duration: {DurationSeconds:F1}s.",
            finishedAt, status, (finishedAt - startedAt).TotalSeconds);

        return new ExportRunResult(
            startedAt,
            finishedAt,
            status,
            enabledPhases.Where(phases.ContainsKey).Select(kind => phases[kind]).ToList());
    }

    private static PhaseResult Merge(PhaseResult forward, PhaseResult deletions) => new(
        forward.Kind,
        forward.Created + deletions.Created,
        forward.Updated + deletions.Updated,
        forward.Deleted + deletions.Deleted,
        forward.Skipped + deletions.Skipped,
        forward.Failed + deletions.Failed);

    private bool IsPhaseEnabled(EntityKind kind)
    {
        var sources = _config.Value.Sources;
        var targets = _config.Value.Targets;

        return kind switch
        {
            EntityKind.ConceptDescription => sources.ConceptDescriptions.Enabled && targets.ConceptDescriptions.Enabled,
            EntityKind.Submodel => sources.Submodels.Enabled && targets.Submodels.Enabled,
            EntityKind.SubmodelDescriptor => sources.SubmodelDescriptors.Enabled && targets.SubmodelDescriptors.Enabled,
            EntityKind.Shell => sources.Shells.Enabled && targets.Shells.Enabled,
            EntityKind.ShellDescriptor => sources.ShellDescriptors.Enabled && targets.ShellDescriptors.Enabled,
            _ => false
        };
    }
}

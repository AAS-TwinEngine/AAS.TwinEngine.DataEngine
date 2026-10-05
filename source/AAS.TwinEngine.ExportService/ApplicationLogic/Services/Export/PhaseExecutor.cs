using AAS.TwinEngine.ExportService.ApplicationLogic.Exceptions;
using AAS.TwinEngine.ExportService.ApplicationLogic.Observability;
using AAS.TwinEngine.ExportService.ApplicationLogic.Services.Crud;
using AAS.TwinEngine.ExportService.ApplicationLogic.Services.State;
using AAS.TwinEngine.ExportService.DomainModel;

using Microsoft.Extensions.Logging;

namespace AAS.TwinEngine.ExportService.ApplicationLogic.Services.Export;

public sealed class PhaseExecutor : IPhaseExecutor
{
    private readonly ISourceEntityReader _sourceReader;
    private readonly ITargetEntityWriter _targetWriter;
    private readonly IStateStore _stateStore;
    private readonly ICrudDecisionMaker _decisionMaker;
    private readonly ILogger<PhaseExecutor> _logger;

    public PhaseExecutor(
        ISourceEntityReader sourceReader,
        ITargetEntityWriter targetWriter,
        IStateStore stateStore,
        ICrudDecisionMaker decisionMaker,
        ILogger<PhaseExecutor> logger)
    {
        _sourceReader = sourceReader;
        _targetWriter = targetWriter;
        _stateStore = stateStore;
        _decisionMaker = decisionMaker;
        _logger = logger;
    }

    public Task<PhaseResult> ExecuteAsync(EntityKind kind, CancellationToken cancellationToken) =>
        ExecuteAsync(kind, decision => decision.Operation != ExportOperation.Delete, cancellationToken);

    public Task<PhaseResult> ExecuteDeletionsAsync(EntityKind kind, CancellationToken cancellationToken) =>
        ExecuteAsync(kind, decision => decision.Operation == ExportOperation.Delete, cancellationToken);

    private async Task<PhaseResult> ExecuteAsync(
        EntityKind kind,
        Func<CrudDecision, bool> shouldApply,
        CancellationToken cancellationToken)
    {
        using var phaseSpan = ExportServiceTracing.StartSpan(
            ExportServiceTracing.Spans.ExportPhase,
            ExportServiceTracing.Attributes.EntityKind,
            kind.ToString());

        _logger.LogInformation("Starting export phase for {EntityKind}", kind);

        IReadOnlyList<SourceEntity> sourceEntities;
        IReadOnlyList<ExportedEntity> managedState;

        using (var fetchSpan = ExportServiceTracing.StartSpan(ExportServiceTracing.Spans.FetchSourceEntities))
        {
            _ = fetchSpan?.SetTag(ExportServiceTracing.Attributes.EntityKind, kind.ToString());
            sourceEntities = await _sourceReader.ReadAllAsync(kind, cancellationToken).ConfigureAwait(false);
        }

        managedState = await _stateStore.LoadAsync(kind, cancellationToken).ConfigureAwait(false);

        IReadOnlyList<CrudDecision> decisions;
        using (var decisionSpan = ExportServiceTracing.StartSpan(ExportServiceTracing.Spans.DecideCrudOperations))
        {
            _ = decisionSpan?.SetTag(ExportServiceTracing.Attributes.EntityKind, kind.ToString());
            decisions = _decisionMaker.Decide(kind, sourceEntities, managedState);
        }

        var created = 0;
        var updated = 0;
        var deleted = 0;
        var skipped = 0;
        var failed = 0;

        foreach (var decision in decisions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!shouldApply(decision))
            {
                continue;
            }

            if (decision.Operation == ExportOperation.Skip)
            {
                skipped++;
                continue;
            }

            try
            {
                var applied = await ApplyAsync(decision, cancellationToken).ConfigureAwait(false);

                switch (decision.Operation)
                {
                    case ExportOperation.Create: created++; break;
                    case ExportOperation.Update: updated++; break;
                    case ExportOperation.Delete:
                        if (applied)
                        {
                            deleted++;
                        }
                        else
                        {
                            // Source still reports the entity as present — deletion was intentionally skipped.
                            skipped++;
                        }

                        break;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogError(
                    ex,
                    "Failed to {Operation} {EntityKind} {Identifier}. Continuing with remaining entities.",
                    decision.Operation,
                    kind,
                    decision.Identifier);
            }
        }

        var result = new PhaseResult(kind, created, updated, deleted, skipped, failed);
        _ = phaseSpan?.SetTag(ExportServiceTracing.Attributes.Created, created);
        _ = phaseSpan?.SetTag(ExportServiceTracing.Attributes.Updated, updated);
        _ = phaseSpan?.SetTag(ExportServiceTracing.Attributes.Deleted, deleted);
        _ = phaseSpan?.SetTag(ExportServiceTracing.Attributes.Skipped, skipped);
        _ = phaseSpan?.SetTag(ExportServiceTracing.Attributes.Failed, failed);

        _logger.LogInformation(
            "Phase {EntityKind} finished. Created={Created} Updated={Updated} Deleted={Deleted} Skipped={Skipped} Failed={Failed}",
            kind, created, updated, deleted, skipped, failed);

        return result;
    }

    private async Task<bool> ApplyAsync(CrudDecision decision, CancellationToken cancellationToken)
    {
        using var writeSpan = ExportServiceTracing.StartSpan(ExportServiceTracing.Spans.WriteEntityToTarget);
        _ = writeSpan?.SetTag(ExportServiceTracing.Attributes.EntityKind, decision.Kind.ToString());
        _ = writeSpan?.SetTag(ExportServiceTracing.Attributes.EntityIdentifier, decision.Identifier);
        _ = writeSpan?.SetTag(ExportServiceTracing.Attributes.Operation, decision.Operation.ToString());

        switch (decision.Operation)
        {
            case ExportOperation.Create:
                await _targetWriter.CreateAsync(decision.Kind, decision.SourceEntity!, cancellationToken).ConfigureAwait(false);
                await _stateStore.UpsertAsync(
                    new ExportedEntity(
                        decision.Kind,
                        decision.SourceEntity!.Identifier,
                        DateTimeOffset.UtcNow,
                        DateTimeOffset.UtcNow,
                        decision.SourceEntity.ContentHash),
                    cancellationToken).ConfigureAwait(false);
                return true;

            case ExportOperation.Update:
                await _targetWriter.UpdateAsync(decision.Kind, decision.SourceEntity!, cancellationToken).ConfigureAwait(false);
                await _stateStore.UpsertAsync(
                    new ExportedEntity(
                        decision.Kind,
                        decision.SourceEntity!.Identifier,
                        DateTimeOffset.UtcNow,
                        DateTimeOffset.UtcNow,
                        decision.SourceEntity.ContentHash),
                    cancellationToken).ConfigureAwait(false);
                return true;

            case ExportOperation.Delete:
                return await ApplyDeletionAsync(decision, cancellationToken).ConfigureAwait(false);

            default:
                return false;
        }
    }

    /// <summary>
    /// Verifies a deletion candidate against the source by GET-by-id before touching the target.
    /// Only deletes when the source confirms the entity is gone (404). If the source still returns
    /// the entity, the deletion is skipped (returns <c>false</c>). A verification failure throws
    /// <see cref="SourceUnavailableException"/>, which is counted as a per-entity failure so the
    /// entity is retried on a future run and its ownership state is preserved.
    /// </summary>
    private async Task<bool> ApplyDeletionAsync(CrudDecision decision, CancellationToken cancellationToken)
    {
        using var verifySpan = ExportServiceTracing.StartSpan(ExportServiceTracing.Spans.VerifyDeletionCandidate);
        _ = verifySpan?.SetTag(ExportServiceTracing.Attributes.EntityKind, decision.Kind.ToString());
        _ = verifySpan?.SetTag(ExportServiceTracing.Attributes.EntityIdentifier, decision.Identifier);

        _logger.LogInformation(
            "Verifying deletion candidate {EntityKind} {Identifier} via source GET-by-id.",
            decision.Kind, decision.Identifier);

        var stillPresent = await _sourceReader.GetByIdAsync(decision.Kind, decision.Identifier, cancellationToken).ConfigureAwait(false);

        if (stillPresent is not null)
        {
            // Pagination/GET-by-id discrepancy: the paginated read missed an entity that still exists.
            _logger.LogWarning(
                "Deletion verification for {EntityKind} {Identifier}: source still reports the entity as present. " +
                "Skipping target deletion and keeping ownership state; reconcile the paginated-read vs GET-by-id discrepancy.",
                decision.Kind, decision.Identifier);
            return false;
        }

        _logger.LogInformation(
            "Deletion verification for {EntityKind} {Identifier}: source confirmed the entity is deleted. Deleting from target.",
            decision.Kind, decision.Identifier);

        await _targetWriter.DeleteAsync(decision.Kind, decision.Identifier, cancellationToken).ConfigureAwait(false);
        await _stateStore.DeleteAsync(decision.Kind, decision.Identifier, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Deletion complete for {EntityKind} {Identifier}: removed from target and ownership state.",
            decision.Kind, decision.Identifier);

        return true;
    }
}

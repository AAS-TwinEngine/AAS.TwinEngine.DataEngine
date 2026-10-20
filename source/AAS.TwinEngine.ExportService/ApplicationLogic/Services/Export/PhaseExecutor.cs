using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using AAS.TwinEngine.ExportService.ApplicationLogic.Exceptions;
using AAS.TwinEngine.ExportService.ApplicationLogic.Observability;
using AAS.TwinEngine.ExportService.ApplicationLogic.Services.Crud;
using AAS.TwinEngine.ExportService.ApplicationLogic.Services.State;
using AAS.TwinEngine.ExportService.DomainModel;
using AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.ExportService.ApplicationLogic.Services.Export;

public sealed class PhaseExecutor(
    ISourceEntityReader sourceReader,
    ITargetEntityWriter targetWriter,
    IStateStore stateStore,
    ICrudDecisionMaker decisionMaker,
    IOptions<ExportServiceConfig> config,
    ILogger<PhaseExecutor> logger) : IPhaseExecutor
{
    private readonly ISourceEntityReader _sourceReader = sourceReader;
    private readonly ITargetEntityWriter _targetWriter = targetWriter;
    private readonly IStateStore _stateStore = stateStore;
    private readonly ICrudDecisionMaker _decisionMaker = decisionMaker;
    private readonly int _maxDegreeOfParallelism = Math.Max(1, config.Value.Performance.MaxDegreeOfParallelism);
    private readonly ILogger<PhaseExecutor> _logger = logger;

    public Task<PhaseResult> ExecuteAsync(EntityKind kind, CancellationToken cancellationToken) => ExecuteAsync(kind, false, cancellationToken);

    public Task<PhaseResult> ExecuteDeletionsAsync(EntityKind kind, CancellationToken cancellationToken) => ExecuteAsync(kind, true, cancellationToken);

    private async Task<PhaseResult> ExecuteAsync(EntityKind kind, bool deletionsOnly, CancellationToken cancellationToken)
    {
        using var phaseSpan = ExportServiceTracing.StartSpan(
            ExportServiceTracing.Spans.ExportPhase,
            ExportServiceTracing.Attributes.EntityKind,
            kind.ToString());

        _logger.LogInformation("Starting export phase for {EntityKind}", kind);

        var managedState = await _stateStore.LoadAsync(kind, cancellationToken).ConfigureAwait(false);
        var counters = new PhaseCounters();
        var sourceIdentifiers = deletionsOnly ? new HashSet<string>(StringComparer.Ordinal) : null;
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = _maxDegreeOfParallelism,
            CancellationToken = cancellationToken
        };

        await ProcessSourcePagesAsync(kind, managedState, sourceIdentifiers, parallelOptions, counters, cancellationToken).ConfigureAwait(false);

        if (sourceIdentifiers is not null)
        {
            await ApplyMissingEntitiesAsync(kind, managedState, sourceIdentifiers, parallelOptions, counters).ConfigureAwait(false);
        }

        return LogPhaseResult(kind, counters, phaseSpan);
    }

    private async Task ProcessSourcePagesAsync(
        EntityKind kind,
        IReadOnlyList<ExportedEntity> managedState,
        HashSet<string>? sourceIdentifiers,
        ParallelOptions parallelOptions,
        PhaseCounters counters,
        CancellationToken cancellationToken)
    {
        var stateByIdentifier = managedState.ToDictionary(entity => entity.Identifier, StringComparer.Ordinal);
        await using var pages = _sourceReader.ReadPagesAsync(kind, cancellationToken).GetAsyncEnumerator(cancellationToken);

        while (await MoveToNextPageAsync(kind, pages).ConfigureAwait(false))
        {
            if (sourceIdentifiers is not null)
            {
                AddSourceIdentifiers(pages.Current, sourceIdentifiers);
                continue;
            }

            await ProcessForwardPageAsync(kind, pages.Current, stateByIdentifier, parallelOptions, counters)
                .ConfigureAwait(false);
        }
    }

    private static async Task<bool> MoveToNextPageAsync(EntityKind kind, IAsyncEnumerator<IReadOnlyList<SourceEntity>> pages)
    {
        using var fetchSpan = ExportServiceTracing.StartSpan(ExportServiceTracing.Spans.FetchSourceEntities);
        _ = fetchSpan?.SetTag(ExportServiceTracing.Attributes.EntityKind, kind.ToString());
        return await pages.MoveNextAsync().ConfigureAwait(false);
    }

    private static void AddSourceIdentifiers(IReadOnlyList<SourceEntity> page, HashSet<string> sourceIdentifiers)
    {
        foreach (var source in page)
        {
            _ = sourceIdentifiers.Add(source.Identifier);
        }
    }

    private async Task ProcessForwardPageAsync(
        EntityKind kind,
        IReadOnlyList<SourceEntity> page,
        IReadOnlyDictionary<string, ExportedEntity> stateByIdentifier,
        ParallelOptions parallelOptions,
        PhaseCounters counters)
    {
        var pageState = new List<ExportedEntity>(page.Count);
        foreach (var source in page)
        {
            if (stateByIdentifier.TryGetValue(source.Identifier, out var managed))
            {
                pageState.Add(managed);
            }
        }

        IReadOnlyList<CrudDecision> decisions;
        using (var decisionSpan = ExportServiceTracing.StartSpan(ExportServiceTracing.Spans.DecideCrudOperations))
        {
            _ = decisionSpan?.SetTag(ExportServiceTracing.Attributes.EntityKind, kind.ToString());
            decisions = _decisionMaker.Decide(
                kind,
                page,
                pageState.DistinctBy(entity => entity.Identifier).ToArray());
        }

        await ApplyDecisionsAsync(
            kind,
            decisions.Where(decision => decision.Operation != ExportOperation.Delete),
            parallelOptions,
            counters).ConfigureAwait(false);
    }

    private async Task ApplyMissingEntitiesAsync(
        EntityKind kind,
        IReadOnlyList<ExportedEntity> managedState,
        HashSet<string> sourceIdentifiers,
        ParallelOptions parallelOptions,
        PhaseCounters counters)
    {
        var missingState = managedState.Where(entity => !sourceIdentifiers.Contains(entity.Identifier)).ToArray();
        IReadOnlyList<CrudDecision> decisions;
        using (var decisionSpan = ExportServiceTracing.StartSpan(ExportServiceTracing.Spans.DecideCrudOperations))
        {
            _ = decisionSpan?.SetTag(ExportServiceTracing.Attributes.EntityKind, kind.ToString());
            decisions = _decisionMaker.Decide(kind, Array.Empty<SourceEntity>(), missingState);
        }

        await ApplyDecisionsAsync(
            kind,
            decisions.Where(decision => decision.Operation == ExportOperation.Delete),
            parallelOptions,
            counters).ConfigureAwait(false);
    }

    private Task ApplyDecisionsAsync(
        EntityKind kind,
        IEnumerable<CrudDecision> decisions,
        ParallelOptions parallelOptions,
        PhaseCounters counters)
    {
        return Parallel.ForEachAsync(
            decisions,
            parallelOptions,
            (decision, token) => ApplyDecisionAsync(kind, decision, counters, token));
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Entity failures are counted and logged while the phase continues; cancellation is rethrown.")]
    private async ValueTask ApplyDecisionAsync(
        EntityKind kind,
        CrudDecision decision,
        PhaseCounters counters,
        CancellationToken cancellationToken)
    {
        if (decision.Operation == ExportOperation.Skip)
        {
            counters.Record(AppliedOutcome.Skipped);
            return;
        }

        try
        {
            counters.Record(await ApplyAsync(decision, cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            counters.RecordFailure();
            _logger.LogError(
                ex,
                "Failed to {Operation} {EntityKind} {Identifier}. Continuing with remaining entities.",
                decision.Operation,
                kind,
                decision.Identifier);
        }
    }

    private PhaseResult LogPhaseResult(EntityKind kind, PhaseCounters counters, Activity? phaseSpan)
    {
        var result = new PhaseResult(kind, counters.Created, counters.Updated, counters.Deleted, counters.Skipped, counters.Failed);
        _ = phaseSpan?.SetTag(ExportServiceTracing.Attributes.Created, counters.Created);
        _ = phaseSpan?.SetTag(ExportServiceTracing.Attributes.Updated, counters.Updated);
        _ = phaseSpan?.SetTag(ExportServiceTracing.Attributes.Deleted, counters.Deleted);
        _ = phaseSpan?.SetTag(ExportServiceTracing.Attributes.Skipped, counters.Skipped);
        _ = phaseSpan?.SetTag(ExportServiceTracing.Attributes.Failed, counters.Failed);

        _logger.LogInformation(
            "Phase {EntityKind} finished. Created={Created} Updated={Updated} Deleted={Deleted} Skipped={Skipped} Failed={Failed}",
            kind, counters.Created, counters.Updated, counters.Deleted, counters.Skipped, counters.Failed);

        return result;
    }

    private async Task<AppliedOutcome> ApplyAsync(CrudDecision decision, CancellationToken cancellationToken)
    {
        using var writeSpan = ExportServiceTracing.StartSpan(ExportServiceTracing.Spans.WriteEntityToTarget);
        _ = writeSpan?.SetTag(ExportServiceTracing.Attributes.EntityKind, decision.Kind.ToString());
        _ = writeSpan?.SetTag(ExportServiceTracing.Attributes.EntityIdentifier, decision.Identifier);
        _ = writeSpan?.SetTag(ExportServiceTracing.Attributes.Operation, decision.Operation.ToString());

        switch (decision.Operation)
        {
            case ExportOperation.Create:
                return await ApplyCreationAsync(decision, cancellationToken).ConfigureAwait(false);

            case ExportOperation.Update:
                await _targetWriter.UpdateAsync(decision.Kind, decision.SourceEntity!, cancellationToken).ConfigureAwait(false);
                await RecordOwnershipAsync(decision, cancellationToken).ConfigureAwait(false);
                return AppliedOutcome.Updated;

            case ExportOperation.Delete:
                return await ApplyDeletionAsync(decision, cancellationToken).ConfigureAwait(false);

            default:
                return AppliedOutcome.Skipped;
        }
    }

    /// <summary>
    /// Creates the entity in the target. A conflict means the target already holds that
    /// identifier while the exporter has no ownership record for it, which is only reconciled
    /// when <see cref="CanAdoptConflictingEntityAsync"/> proves the exporter produced it.
    /// </summary>
    private async Task<AppliedOutcome> ApplyCreationAsync(CrudDecision decision, CancellationToken cancellationToken)
    {
        try
        {
            await _targetWriter.CreateAsync(decision.Kind, decision.SourceEntity!, cancellationToken).ConfigureAwait(false);
        }
        catch (TargetEntityConflictException ex)
        {
            if (!await CanAdoptConflictingEntityAsync(decision, cancellationToken).ConfigureAwait(false))
            {
                _logger.LogWarning(
                    ex,
                    "Create for {EntityKind} {Identifier} conflicts with an existing target entity that is not exporter-managed. " +
                    "Refusing to modify it; resolve the identifier collision in the target.",
                    decision.Kind,
                    decision.Identifier);
                throw;
            }

            _logger.LogInformation(
                "Create for {EntityKind} {Identifier} conflicted with a descriptor the target generated for a shell or submodel " +
                "this exporter owns. Reconciling it with the source and recording ownership.",
                decision.Kind,
                decision.Identifier);

            await _targetWriter.UpdateAsync(decision.Kind, decision.SourceEntity!, cancellationToken).ConfigureAwait(false);
            await RecordOwnershipAsync(decision, cancellationToken).ConfigureAwait(false);
            return AppliedOutcome.Updated;
        }

        await RecordOwnershipAsync(decision, cancellationToken).ConfigureAwait(false);
        return AppliedOutcome.Created;
    }

    /// <summary>
    /// Decides whether a conflicting target entity may be adopted by the exporter.
    /// Only registry descriptors qualify: a target that integrates its registry creates the
    /// descriptor itself when the exporter creates the shell or submodel it describes, so the
    /// exporter never gets the chance to record ownership of it. Because a descriptor carries
    /// the same identifier as the entity it describes, an ownership record for that entity
    /// proves the conflicting descriptor is a product of this exporter's own write. Any other
    /// conflict belongs to data the exporter does not manage and is never modified.
    /// </summary>
    private async Task<bool> CanAdoptConflictingEntityAsync(CrudDecision decision, CancellationToken cancellationToken)
    {
        if (decision.Kind.DescribedEntityKind() is not { } describedKind)
        {
            return false;
        }

        var describedEntity = await _stateStore
            .GetAsync(describedKind, decision.Identifier, cancellationToken)
            .ConfigureAwait(false);

        return describedEntity is not null;
    }

    private Task RecordOwnershipAsync(CrudDecision decision, CancellationToken cancellationToken)
    {
        return _stateStore.UpsertAsync(
            new ExportedEntity(
                decision.Kind,
                decision.SourceEntity!.Identifier,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                decision.SourceEntity.ContentHash),
            cancellationToken);
    }

    /// <summary>
    /// Verifies a deletion candidate against the source by GET-by-id before touching the target.
    /// Only deletes when the source confirms the entity is gone (404). If the source still returns
    /// the entity, the deletion is skipped. A verification failure throws
    /// <see cref="SourceUnavailableException"/>, which is counted as a per-entity failure so the
    /// entity is retried on a future run and its ownership state is preserved.
    /// </summary>
    private async Task<AppliedOutcome> ApplyDeletionAsync(CrudDecision decision, CancellationToken cancellationToken)
    {
        using var verifySpan = ExportServiceTracing.StartSpan(ExportServiceTracing.Spans.VerifyDeletionCandidate);
        _ = verifySpan?.SetTag(ExportServiceTracing.Attributes.EntityKind, decision.Kind.ToString());
        _ = verifySpan?.SetTag(ExportServiceTracing.Attributes.EntityIdentifier, decision.Identifier);

        _logger.LogInformation("Verifying deletion candidate {EntityKind} {Identifier} via source GET-by-id.", decision.Kind, decision.Identifier);

        var stillPresent = await _sourceReader.GetByIdAsync(decision.Kind, decision.Identifier, cancellationToken).ConfigureAwait(false);

        if (stillPresent is not null)
        {
            // Pagination/GET-by-id discrepancy: the paginated read missed an entity that still exists.
            _logger.LogWarning(
                "Deletion verification for {EntityKind} {Identifier}: source still reports the entity as present. " +
                "Skipping target deletion and keeping ownership state; reconcile the paginated-read vs GET-by-id discrepancy.",
                decision.Kind, decision.Identifier);
            return AppliedOutcome.Skipped;
        }

        await _targetWriter.DeleteAsync(decision.Kind, decision.Identifier, cancellationToken).ConfigureAwait(false);
        await _stateStore.DeleteAsync(decision.Kind, decision.Identifier, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Deletion complete for {EntityKind} {Identifier}: source confirmed the entity is deleted; removed from target and ownership state.",
            decision.Kind, decision.Identifier);

        return AppliedOutcome.Deleted;
    }

    /// <summary>
    /// The operation actually carried out for a decision, which can differ from the planned
    /// operation when a deletion is skipped or a create is reconciled into an update.
    /// </summary>
    private enum AppliedOutcome
    {
        Created,
        Updated,
        Deleted,
        Skipped
    }

    private sealed class PhaseCounters
    {
        private int _created;
        private int _updated;
        private int _deleted;
        private int _skipped;
        private int _failed;

        public int Created => Volatile.Read(ref _created);

        public int Updated => Volatile.Read(ref _updated);

        public int Deleted => Volatile.Read(ref _deleted);

        public int Skipped => Volatile.Read(ref _skipped);

        public int Failed => Volatile.Read(ref _failed);

        public void Record(AppliedOutcome outcome)
        {
            switch (outcome)
            {
                case AppliedOutcome.Created:
                    _ = Interlocked.Increment(ref _created);
                    break;
                case AppliedOutcome.Updated:
                    _ = Interlocked.Increment(ref _updated);
                    break;
                case AppliedOutcome.Deleted:
                    _ = Interlocked.Increment(ref _deleted);
                    break;
                case AppliedOutcome.Skipped:
                    _ = Interlocked.Increment(ref _skipped);
                    break;
            }
        }

        public void RecordFailure() => _ = Interlocked.Increment(ref _failed);
    }
}

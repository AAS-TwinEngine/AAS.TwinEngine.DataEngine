using AAS.TwinEngine.ExportService.ApplicationLogic.Exceptions;
using AAS.TwinEngine.ExportService.ApplicationLogic.Services.Crud;
using AAS.TwinEngine.ExportService.ApplicationLogic.Services.Export;
using AAS.TwinEngine.ExportService.ApplicationLogic.Services.State;
using AAS.TwinEngine.ExportService.DomainModel;
using AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AAS.TwinEngine.ExportService.UnitTests.ApplicationLogic.Services.Export;

public class PhaseExecutorTests
{
    private readonly ISourceEntityReader _sourceReader = Substitute.For<ISourceEntityReader>();
    private readonly ITargetEntityWriter _targetWriter = Substitute.For<ITargetEntityWriter>();
    private readonly IStateStore _stateStore = Substitute.For<IStateStore>();
    private readonly ICrudDecisionMaker _decisionMaker = Substitute.For<ICrudDecisionMaker>();
    private readonly ILogger<PhaseExecutor> _logger = Substitute.For<ILogger<PhaseExecutor>>();
    private readonly ExportServiceConfig _config = new();

    private PhaseExecutor CreateSut() =>
        new(_sourceReader, _targetWriter, _stateStore, _decisionMaker, Options.Create(_config), _logger);

    [Fact]
    public async Task ExecuteAsync_WhenAllOperationsSucceed_AppliesOperationsAndReturnsCounts()
    {
        // Arrange
        var sut = CreateSut();
        var kind = EntityKind.Shell;

        var sourceCreate = new SourceEntity("id_create", "{\"id\":\"id_create\"}");
        var sourceUpdate = new SourceEntity("id_update", "{\"id\":\"id_update\"}");
        var sources = new[] { sourceCreate, sourceUpdate };

        var stateExisting = new ExportedEntity(kind, "id_update", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var stateDelete = new ExportedEntity(kind, "id_delete", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var state = new[] { stateExisting, stateDelete };

        _sourceReader.ReadAllAsync(kind, Arg.Any<CancellationToken>()).Returns(sources);
        _stateStore.LoadAsync(kind, Arg.Any<CancellationToken>()).Returns(state);

        var decisions = new List<CrudDecision>
        {
            new(ExportOperation.Create, kind, "id_create", sourceCreate),
            new(ExportOperation.Update, kind, "id_update", sourceUpdate),
            new(ExportOperation.Delete, kind, "id_delete", null),
            new(ExportOperation.Skip, kind, "id_skip", null)
        };
        _decisionMaker.Decide(kind, sources, state).Returns(decisions);

        // Act
        var result = await sut.ExecuteAsync(kind, CancellationToken.None);

        // Assert
        Assert.Equal(kind, result.Kind);
        Assert.Equal(1, result.Created);
        Assert.Equal(1, result.Updated);
        Assert.Equal(0, result.Deleted);
        Assert.Equal(1, result.Skipped);
        Assert.Equal(0, result.Failed);
        Assert.False(result.HasFailures);

        await _targetWriter.Received(1).CreateAsync(kind, sourceCreate, Arg.Any<CancellationToken>());
        await _stateStore.Received(1).UpsertAsync(
            Arg.Is<ExportedEntity>(e => e.Kind == kind && e.Identifier == "id_create" && e.ContentHash == sourceCreate.ContentHash),
            Arg.Any<CancellationToken>());

        await _targetWriter.Received(1).UpdateAsync(kind, sourceUpdate, Arg.Any<CancellationToken>());
        await _stateStore.Received(1).UpsertAsync(
            Arg.Is<ExportedEntity>(e => e.Kind == kind && e.Identifier == "id_update" && e.ContentHash == sourceUpdate.ContentHash),
            Arg.Any<CancellationToken>());
        await _stateStore.Received(2).UpsertAsync(Arg.Any<ExportedEntity>(), Arg.Any<CancellationToken>());

        await _targetWriter.DidNotReceive().DeleteAsync(kind, "id_delete", Arg.Any<CancellationToken>());
        await _stateStore.DidNotReceive().DeleteAsync(kind, "id_delete", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteDeletionsAsync_AppliesOnlyDeleteDecisions()
    {
        // Arrange
        var sut = CreateSut();
        var kind = EntityKind.Submodel;
        var source = new SourceEntity("id_update", "{\"id\":\"id_update\"}");
        var decisions = new[]
        {
            new CrudDecision(ExportOperation.Update, kind, "id_update", source),
            new CrudDecision(ExportOperation.Delete, kind, "id_delete", null)
        };

        _sourceReader.ReadAllAsync(kind, Arg.Any<CancellationToken>()).Returns(new[] { source });
        _stateStore.LoadAsync(kind, Arg.Any<CancellationToken>()).Returns(Array.Empty<ExportedEntity>());
        _decisionMaker.Decide(kind, Arg.Any<IReadOnlyList<SourceEntity>>(), Arg.Any<IReadOnlyList<ExportedEntity>>())
            .Returns(decisions);

        // Source confirms the entity is gone (404 → null) so the deletion proceeds.
        _sourceReader.GetByIdAsync(kind, "id_delete", Arg.Any<CancellationToken>()).Returns((SourceEntity?)null);

        // Act
        var result = await sut.ExecuteDeletionsAsync(kind, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.Deleted);
        Assert.Equal(0, result.Updated);
        await _sourceReader.Received(1).GetByIdAsync(kind, "id_delete", Arg.Any<CancellationToken>());
        await _targetWriter.Received(1).DeleteAsync(kind, "id_delete", Arg.Any<CancellationToken>());
        await _stateStore.Received(1).DeleteAsync(kind, "id_delete", Arg.Any<CancellationToken>());
        await _targetWriter.DidNotReceive().UpdateAsync(kind, source, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteDeletionsAsync_WhenSourceConfirmsDeleted_DeletesTargetAndState()
    {
        // Arrange
        var sut = CreateSut();
        var kind = EntityKind.Submodel;
        var decisions = new[] { new CrudDecision(ExportOperation.Delete, kind, "id_gone", null) };

        _sourceReader.ReadAllAsync(kind, Arg.Any<CancellationToken>()).Returns(Array.Empty<SourceEntity>());
        _stateStore.LoadAsync(kind, Arg.Any<CancellationToken>()).Returns(Array.Empty<ExportedEntity>());
        _decisionMaker.Decide(kind, Arg.Any<IReadOnlyList<SourceEntity>>(), Arg.Any<IReadOnlyList<ExportedEntity>>())
            .Returns(decisions);
        _sourceReader.GetByIdAsync(kind, "id_gone", Arg.Any<CancellationToken>()).Returns((SourceEntity?)null);

        // Act
        var result = await sut.ExecuteDeletionsAsync(kind, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.Deleted);
        Assert.Equal(0, result.Skipped);
        Assert.Equal(0, result.Failed);
        await _targetWriter.Received(1).DeleteAsync(kind, "id_gone", Arg.Any<CancellationToken>());
        await _stateStore.Received(1).DeleteAsync(kind, "id_gone", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteDeletionsAsync_WhenSourceStillReturnsEntity_SkipsDeletionAndKeepsState()
    {
        // Arrange
        var sut = CreateSut();
        var kind = EntityKind.Submodel;
        var decisions = new[] { new CrudDecision(ExportOperation.Delete, kind, "id_present", null) };

        _sourceReader.ReadAllAsync(kind, Arg.Any<CancellationToken>()).Returns(Array.Empty<SourceEntity>());
        _stateStore.LoadAsync(kind, Arg.Any<CancellationToken>()).Returns(Array.Empty<ExportedEntity>());
        _decisionMaker.Decide(kind, Arg.Any<IReadOnlyList<SourceEntity>>(), Arg.Any<IReadOnlyList<ExportedEntity>>())
            .Returns(decisions);

        // Source still reports the entity as present — pagination/GET-by-id discrepancy.
        _sourceReader.GetByIdAsync(kind, "id_present", Arg.Any<CancellationToken>())
            .Returns(new SourceEntity("id_present", "{\"id\":\"id_present\"}"));

        // Act
        var result = await sut.ExecuteDeletionsAsync(kind, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.Deleted);
        Assert.Equal(1, result.Skipped);
        Assert.Equal(0, result.Failed);
        await _targetWriter.DidNotReceive().DeleteAsync(kind, "id_present", Arg.Any<CancellationToken>());
        await _stateStore.DidNotReceive().DeleteAsync(kind, "id_present", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteDeletionsAsync_WhenVerificationFailsTransiently_DoesNotDeleteAndCountsFailure()
    {
        // Arrange
        var sut = CreateSut();
        var kind = EntityKind.Submodel;
        var decisions = new[] { new CrudDecision(ExportOperation.Delete, kind, "id_unknown", null) };

        _sourceReader.ReadAllAsync(kind, Arg.Any<CancellationToken>()).Returns(Array.Empty<SourceEntity>());
        _stateStore.LoadAsync(kind, Arg.Any<CancellationToken>()).Returns(Array.Empty<ExportedEntity>());
        _decisionMaker.Decide(kind, Arg.Any<IReadOnlyList<SourceEntity>>(), Arg.Any<IReadOnlyList<ExportedEntity>>())
            .Returns(decisions);

        // GET-by-id fails with a server/transient error → existence cannot be determined.
        _sourceReader.GetByIdAsync(kind, "id_unknown", Arg.Any<CancellationToken>())
            .ThrowsAsync(new SourceUnavailableException("Source GET-by-id failed with status 500."));

        // Act
        var result = await sut.ExecuteDeletionsAsync(kind, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.Deleted);
        Assert.Equal(1, result.Failed);
        Assert.True(result.HasFailures);
        await _targetWriter.DidNotReceive().DeleteAsync(kind, "id_unknown", Arg.Any<CancellationToken>());
        await _stateStore.DidNotReceive().DeleteAsync(kind, "id_unknown", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteDeletionsAsync_WithMultipleCandidates_VerifiesEachIndependently()
    {
        // Arrange
        var sut = CreateSut();
        var kind = EntityKind.Submodel;
        var decisions = new[]
        {
            new CrudDecision(ExportOperation.Delete, kind, "id_gone", null),
            new CrudDecision(ExportOperation.Delete, kind, "id_present", null),
            new CrudDecision(ExportOperation.Delete, kind, "id_error", null)
        };

        _sourceReader.ReadAllAsync(kind, Arg.Any<CancellationToken>()).Returns(Array.Empty<SourceEntity>());
        _stateStore.LoadAsync(kind, Arg.Any<CancellationToken>()).Returns(Array.Empty<ExportedEntity>());
        _decisionMaker.Decide(kind, Arg.Any<IReadOnlyList<SourceEntity>>(), Arg.Any<IReadOnlyList<ExportedEntity>>())
            .Returns(decisions);

        _sourceReader.GetByIdAsync(kind, "id_gone", Arg.Any<CancellationToken>()).Returns((SourceEntity?)null);
        _sourceReader.GetByIdAsync(kind, "id_present", Arg.Any<CancellationToken>())
            .Returns(new SourceEntity("id_present", "{\"id\":\"id_present\"}"));
        _sourceReader.GetByIdAsync(kind, "id_error", Arg.Any<CancellationToken>())
            .ThrowsAsync(new SourceUnavailableException("timeout"));

        // Act
        var result = await sut.ExecuteDeletionsAsync(kind, CancellationToken.None);

        // Assert — one deleted, one skipped (still present), one failed (could not verify).
        Assert.Equal(1, result.Deleted);
        Assert.Equal(1, result.Skipped);
        Assert.Equal(1, result.Failed);

        await _targetWriter.Received(1).DeleteAsync(kind, "id_gone", Arg.Any<CancellationToken>());
        await _stateStore.Received(1).DeleteAsync(kind, "id_gone", Arg.Any<CancellationToken>());
        await _targetWriter.DidNotReceive().DeleteAsync(kind, "id_present", Arg.Any<CancellationToken>());
        await _targetWriter.DidNotReceive().DeleteAsync(kind, "id_error", Arg.Any<CancellationToken>());
        await _stateStore.DidNotReceive().DeleteAsync(kind, "id_present", Arg.Any<CancellationToken>());
        await _stateStore.DidNotReceive().DeleteAsync(kind, "id_error", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ForwardPass_DoesNotVerifyOrApplyDeletions()
    {
        // Arrange
        var sut = CreateSut();
        var kind = EntityKind.Submodel;
        var decisions = new[] { new CrudDecision(ExportOperation.Delete, kind, "id_delete", null) };

        _sourceReader.ReadAllAsync(kind, Arg.Any<CancellationToken>()).Returns(Array.Empty<SourceEntity>());
        _stateStore.LoadAsync(kind, Arg.Any<CancellationToken>()).Returns(Array.Empty<ExportedEntity>());
        _decisionMaker.Decide(kind, Arg.Any<IReadOnlyList<SourceEntity>>(), Arg.Any<IReadOnlyList<ExportedEntity>>())
            .Returns(decisions);

        // Act
        var result = await sut.ExecuteAsync(kind, CancellationToken.None);

        // Assert — the forward pass must not touch deletions or verify them.
        Assert.Equal(0, result.Deleted);
        await _sourceReader.DidNotReceive().GetByIdAsync(kind, Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _targetWriter.DidNotReceive().DeleteAsync(kind, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenTargetWriterThrows_IncrementsFailedAndContinuesWithRemainingEntities()
    {
        // Arrange
        var sut = CreateSut();
        var kind = EntityKind.Submodel;

        var sourceFailing = new SourceEntity("id_fail", "{\"id\":\"id_fail\"}");
        var sourceSucceeding = new SourceEntity("id_ok", "{\"id\":\"id_ok\"}");
        var sources = new[] { sourceFailing, sourceSucceeding };

        _sourceReader.ReadAllAsync(kind, Arg.Any<CancellationToken>()).Returns(sources);
        _stateStore.LoadAsync(kind, Arg.Any<CancellationToken>()).Returns(Array.Empty<ExportedEntity>());

        var decisions = new List<CrudDecision>
        {
            new(ExportOperation.Create, kind, "id_fail", sourceFailing),
            new(ExportOperation.Create, kind, "id_ok", sourceSucceeding)
        };
        _decisionMaker.Decide(kind, sources, Arg.Any<IReadOnlyList<ExportedEntity>>()).Returns(decisions);

        _targetWriter.CreateAsync(kind, sourceFailing, Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Target server down"));

        // Act
        var result = await sut.ExecuteAsync(kind, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.Created);
        Assert.Equal(1, result.Failed);
        Assert.True(result.HasFailures);

        await _targetWriter.Received(1).CreateAsync(kind, sourceSucceeding, Arg.Any<CancellationToken>());
        await _stateStore.Received(1).UpsertAsync(
            Arg.Is<ExportedEntity>(e => e.Identifier == "id_ok"),
            Arg.Any<CancellationToken>());
        await _stateStore.DidNotReceive().UpsertAsync(
            Arg.Is<ExportedEntity>(e => e.Identifier == "id_fail"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenStateStoreThrowsOnApply_IncrementsFailedAndContinues()
    {
        // Arrange
        var sut = CreateSut();
        var kind = EntityKind.ConceptDescription;

        var source = new SourceEntity("id_db_fail", "{\"id\":\"id_db_fail\"}");
        _sourceReader.ReadAllAsync(kind, Arg.Any<CancellationToken>()).Returns(new[] { source });
        _stateStore.LoadAsync(kind, Arg.Any<CancellationToken>()).Returns(Array.Empty<ExportedEntity>());

        var decisions = new List<CrudDecision>
        {
            new(ExportOperation.Create, kind, "id_db_fail", source)
        };
        _decisionMaker.Decide(kind, Arg.Any<IReadOnlyList<SourceEntity>>(), Arg.Any<IReadOnlyList<ExportedEntity>>())
            .Returns(decisions);

        _stateStore.UpsertAsync(Arg.Any<ExportedEntity>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("DB error"));

        // Act
        var result = await sut.ExecuteAsync(kind, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.Created);
        Assert.Equal(1, result.Failed);
        Assert.True(result.HasFailures);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOperationCanceledExceptionThrownDuringApply_RethrowsImmediately()
    {
        // Arrange
        var sut = CreateSut();
        var kind = EntityKind.ShellDescriptor;
        var source = new SourceEntity("id_cancel", "{\"id\":\"id_cancel\"}");

        _sourceReader.ReadAllAsync(kind, Arg.Any<CancellationToken>()).Returns(new[] { source });
        _stateStore.LoadAsync(kind, Arg.Any<CancellationToken>()).Returns(Array.Empty<ExportedEntity>());

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var decisions = new List<CrudDecision>
        {
            new(ExportOperation.Create, kind, "id_cancel", source)
        };
        _decisionMaker.Decide(kind, Arg.Any<IReadOnlyList<SourceEntity>>(), Arg.Any<IReadOnlyList<ExportedEntity>>())
            .Returns(decisions);

        _targetWriter.CreateAsync(kind, source, Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => sut.ExecuteAsync(kind, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancellationRequestedBeforeLoop_ThrowsOperationCanceledException()
    {
        // Arrange
        var sut = CreateSut();
        var kind = EntityKind.Shell;

        _sourceReader.ReadAllAsync(kind, Arg.Any<CancellationToken>()).Returns(Array.Empty<SourceEntity>());
        _stateStore.LoadAsync(kind, Arg.Any<CancellationToken>()).Returns(Array.Empty<ExportedEntity>());

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var decisions = new List<CrudDecision>
        {
            new(ExportOperation.Create, kind, "id1", new SourceEntity("id1", "{}"))
        };
        _decisionMaker.Decide(kind, Arg.Any<IReadOnlyList<SourceEntity>>(), Arg.Any<IReadOnlyList<ExportedEntity>>())
            .Returns(decisions);

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.ExecuteAsync(kind, cts.Token));
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmptySourcesAndState_ReturnsZeroCounters()
    {
        // Arrange
        var sut = CreateSut();
        var kind = EntityKind.ConceptDescription;

        _sourceReader.ReadAllAsync(kind, Arg.Any<CancellationToken>()).Returns(Array.Empty<SourceEntity>());
        _stateStore.LoadAsync(kind, Arg.Any<CancellationToken>()).Returns(Array.Empty<ExportedEntity>());
        _decisionMaker.Decide(kind, Arg.Any<IReadOnlyList<SourceEntity>>(), Arg.Any<IReadOnlyList<ExportedEntity>>())
            .Returns(Array.Empty<CrudDecision>());

        // Act
        var result = await sut.ExecuteAsync(kind, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.Created);
        Assert.Equal(0, result.Updated);
        Assert.Equal(0, result.Deleted);
        Assert.Equal(0, result.Skipped);
        Assert.Equal(0, result.Failed);
        Assert.False(result.HasFailures);
    }

    [Fact]
    public async Task ExecuteAsync_WithManyEntities_AppliesConcurrentlyWithinConfiguredLimitAndCountsCorrectly()
    {
        // Arrange
        var kind = EntityKind.Submodel;
        _config.Performance.MaxDegreeOfParallelism = 4;
        var sut = CreateSut();

        var sources = Enumerable.Range(0, 40)
            .Select(i => new SourceEntity($"id{i}", $"{{\"id\":\"id{i}\"}}"))
            .ToArray();

        _sourceReader.ReadAllAsync(kind, Arg.Any<CancellationToken>()).Returns(sources);
        _stateStore.LoadAsync(kind, Arg.Any<CancellationToken>()).Returns(Array.Empty<ExportedEntity>());
        var decisions = sources.Select(s => new CrudDecision(ExportOperation.Create, kind, s.Identifier, s)).ToList();
        _decisionMaker.Decide(kind, sources, Arg.Any<IReadOnlyList<ExportedEntity>>()).Returns(decisions);

        var current = 0;
        var maxObserved = 0;
        _targetWriter.CreateAsync(kind, Arg.Any<SourceEntity>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                var now = Interlocked.Increment(ref current);
                InterlockedMax(ref maxObserved, now);
                await Task.Delay(15);
                Interlocked.Decrement(ref current);
            });

        // Act
        var result = await sut.ExecuteAsync(kind, CancellationToken.None);

        // Assert
        Assert.Equal(40, result.Created);
        Assert.Equal(0, result.Failed);
        await _stateStore.Received(40).UpsertAsync(Arg.Any<ExportedEntity>(), Arg.Any<CancellationToken>());
        Assert.True(maxObserved > 1, "Expected concurrent execution with a degree greater than one.");
        Assert.True(maxObserved <= 4, $"Observed concurrency {maxObserved} exceeded the configured limit of 4.");
    }

    [Fact]
    public async Task ExecuteAsync_WhenMaxDegreeOfParallelismIsOne_AppliesSequentially()
    {
        // Arrange
        var kind = EntityKind.Submodel;
        _config.Performance.MaxDegreeOfParallelism = 1;
        var sut = CreateSut();

        var sources = Enumerable.Range(0, 10)
            .Select(i => new SourceEntity($"id{i}", $"{{\"id\":\"id{i}\"}}"))
            .ToArray();

        _sourceReader.ReadAllAsync(kind, Arg.Any<CancellationToken>()).Returns(sources);
        _stateStore.LoadAsync(kind, Arg.Any<CancellationToken>()).Returns(Array.Empty<ExportedEntity>());
        var decisions = sources.Select(s => new CrudDecision(ExportOperation.Create, kind, s.Identifier, s)).ToList();
        _decisionMaker.Decide(kind, sources, Arg.Any<IReadOnlyList<ExportedEntity>>()).Returns(decisions);

        var current = 0;
        var maxObserved = 0;
        _targetWriter.CreateAsync(kind, Arg.Any<SourceEntity>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                var now = Interlocked.Increment(ref current);
                InterlockedMax(ref maxObserved, now);
                await Task.Delay(5);
                Interlocked.Decrement(ref current);
            });

        // Act
        var result = await sut.ExecuteAsync(kind, CancellationToken.None);

        // Assert
        Assert.Equal(10, result.Created);
        Assert.Equal(1, maxObserved);
    }

    private static void InterlockedMax(ref int target, int candidate)
    {
        int current;
        while (candidate > (current = Volatile.Read(ref target)))
        {
            if (Interlocked.CompareExchange(ref target, candidate, current) == current)
            {
                return;
            }
        }
    }
}

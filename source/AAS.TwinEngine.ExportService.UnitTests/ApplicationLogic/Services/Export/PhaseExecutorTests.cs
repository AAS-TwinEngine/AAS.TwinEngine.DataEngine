using System.Runtime.CompilerServices;

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

    public PhaseExecutorTests()
    {
        _sourceReader.ReadPagesAsync(Arg.Any<EntityKind>(), Arg.Any<CancellationToken>())
            .Returns(call => ReadConfiguredPageAsync(call.Arg<EntityKind>(), call.Arg<CancellationToken>()));
    }

    private async IAsyncEnumerable<IReadOnlyList<SourceEntity>> ReadConfiguredPageAsync(
        EntityKind kind,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return await _sourceReader.ReadAllAsync(kind, cancellationToken);
    }

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
        _decisionMaker.Decide(kind, sources,
                Arg.Is<IReadOnlyList<ExportedEntity>>(entities => entities.Count == 1 && entities[0] == stateExisting))
            .Returns(decisions);

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

    [Theory]
    [InlineData(5000)]
    [InlineData(20000)]
    public async Task ExecuteAsync_WithLargePagedSource_CompletesEachPageBeforeFetchingNext(int entityCount)
    {
        var writer = new CountingTargetWriter();
        _sourceReader.ReadPagesAsync(EntityKind.Shell, Arg.Any<CancellationToken>())
            .Returns(call => GeneratePagesAsync(entityCount, writer, call.Arg<CancellationToken>()));
        _stateStore.LoadAsync(EntityKind.Shell, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ExportedEntity>());
        var sut = new PhaseExecutor(_sourceReader, writer, _stateStore, new CrudDecisionMaker(), Options.Create(_config), _logger);

        var result = await sut.ExecuteAsync(EntityKind.Shell, CancellationToken.None);

        Assert.Equal(entityCount, result.Created);
        Assert.Equal(entityCount, writer.Created);
        Assert.Equal(0, result.Failed);
        await _sourceReader.DidNotReceive().ReadAllAsync(Arg.Any<EntityKind>(), Arg.Any<CancellationToken>());
        await _stateStore.Received(entityCount).UpsertAsync(Arg.Any<ExportedEntity>(), Arg.Any<CancellationToken>());
    }

    private static async IAsyncEnumerable<IReadOnlyList<SourceEntity>> GeneratePagesAsync(
        int entityCount,
        CountingTargetWriter writer,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        WeakReference? firstPayload = null;
        for (var offset = 0; offset < entityCount; offset += 250)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Assert.Equal(offset, writer.Created);
            if (offset == 750)
            {
                _ = GC.GetTotalMemory(forceFullCollection: true);
                Assert.False(firstPayload!.IsAlive, "Completed pages must not retain their source payloads.");
            }

            var page = Enumerable.Range(offset, Math.Min(250, entityCount - offset))
                .Select(index => new SourceEntity($"id{index}", $"{{\"id\":\"id{index}\",\"value\":\"{new string('x', 4096)}\"}}"))
                .ToArray();
            firstPayload ??= new WeakReference(page[0]);
            yield return page;
            await Task.Yield();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteAsync_WhenLaterPageFails_PreservesOwnershipAndNeverDeletes(bool deletionsOnly)
    {
        var source = new SourceEntity("present", "{\"id\":\"present\"}");
        var missing = new ExportedEntity(EntityKind.Shell, "missing", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "old");
        _sourceReader.ReadPagesAsync(EntityKind.Shell, Arg.Any<CancellationToken>())
            .Returns(FailingPagesAsync(source));
        _stateStore.LoadAsync(EntityKind.Shell, Arg.Any<CancellationToken>()).Returns(new[] { missing });
        var sut = new PhaseExecutor(_sourceReader, _targetWriter, _stateStore, new CrudDecisionMaker(), Options.Create(_config), _logger);

        await Assert.ThrowsAsync<SourceUnavailableException>(() => deletionsOnly
            ? sut.ExecuteDeletionsAsync(EntityKind.Shell, CancellationToken.None)
            : sut.ExecuteAsync(EntityKind.Shell, CancellationToken.None));

        await _targetWriter.Received(deletionsOnly ? 0 : 1).CreateAsync(EntityKind.Shell, source, Arg.Any<CancellationToken>());
        await _stateStore.Received(deletionsOnly ? 0 : 1).UpsertAsync(
            Arg.Is<ExportedEntity>(entity => entity.Identifier == "present"), Arg.Any<CancellationToken>());
        await _sourceReader.DidNotReceive().GetByIdAsync(Arg.Any<EntityKind>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _targetWriter.DidNotReceive().DeleteAsync(Arg.Any<EntityKind>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _stateStore.DidNotReceive().DeleteAsync(Arg.Any<EntityKind>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private static async IAsyncEnumerable<IReadOnlyList<SourceEntity>> FailingPagesAsync(SourceEntity source)
    {
        yield return new[] { source };
        await Task.Yield();
        throw new SourceUnavailableException("Second source page failed.");
    }

    [Fact]
    public async Task ExecuteAsync_WithPagedMixedOperations_PreservesDecisionsAndPayloads()
    {
        var create = new SourceEntity("new", "{\"id\":\"new\",\"value\":null}");
        var update = new SourceEntity("changed", "{\"id\":\"changed\",\"value\":42}");
        var skip = new SourceEntity("unchanged", "{\"id\":\"unchanged\"}");
        var managed = new[]
        {
            new ExportedEntity(EntityKind.Shell, update.Identifier, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "old"),
            new ExportedEntity(EntityKind.Shell, skip.Identifier, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, skip.ContentHash)
        };
        _sourceReader.ReadPagesAsync(EntityKind.Shell, Arg.Any<CancellationToken>()).Returns(MixedPagesAsync());
        _stateStore.LoadAsync(EntityKind.Shell, Arg.Any<CancellationToken>()).Returns(managed);
        var sut = new PhaseExecutor(_sourceReader, _targetWriter, _stateStore, new CrudDecisionMaker(), Options.Create(_config), _logger);

        var result = await sut.ExecuteAsync(EntityKind.Shell, CancellationToken.None);

        Assert.Equal(1, result.Created);
        Assert.Equal(1, result.Updated);
        Assert.Equal(1, result.Skipped);
        Assert.Equal(0, result.Failed);
        await _targetWriter.Received(1).CreateAsync(EntityKind.Shell, create, Arg.Any<CancellationToken>());
        await _targetWriter.Received(1).UpdateAsync(EntityKind.Shell, update, Arg.Any<CancellationToken>());
        await _stateStore.Received(1).UpsertAsync(Arg.Is<ExportedEntity>(entity =>
            entity.Identifier == update.Identifier && entity.ContentHash == update.ContentHash), Arg.Any<CancellationToken>());

        async IAsyncEnumerable<IReadOnlyList<SourceEntity>> MixedPagesAsync()
        {
            yield return new[] { create, skip };
            await Task.Yield();
            yield return new[] { update };
        }
    }

    [Fact]
    public async Task ExecuteDeletionsAsync_WithCompletePagedSource_VerifiesOnlyMissingManagedEntities()
    {
        string[] identifiers = ["present1", "present2", "missing"];
        var managed = identifiers
            .Select(identifier => new ExportedEntity(EntityKind.Shell, identifier, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "old"))
            .ToArray();
        _sourceReader.ReadPagesAsync(EntityKind.Shell, Arg.Any<CancellationToken>()).Returns(PagesAsync());
        _stateStore.LoadAsync(EntityKind.Shell, Arg.Any<CancellationToken>()).Returns(managed);
        _sourceReader.GetByIdAsync(EntityKind.Shell, "missing", Arg.Any<CancellationToken>()).Returns((SourceEntity?)null);
        var sut = new PhaseExecutor(_sourceReader, _targetWriter, _stateStore, new CrudDecisionMaker(), Options.Create(_config), _logger);

        var result = await sut.ExecuteDeletionsAsync(EntityKind.Shell, CancellationToken.None);

        Assert.Equal(1, result.Deleted);
        Assert.Equal(0, result.Failed);
        await _sourceReader.Received(1).GetByIdAsync(EntityKind.Shell, Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _targetWriter.Received(1).DeleteAsync(EntityKind.Shell, "missing", Arg.Any<CancellationToken>());
        await _stateStore.Received(1).DeleteAsync(EntityKind.Shell, "missing", Arg.Any<CancellationToken>());

        static async IAsyncEnumerable<IReadOnlyList<SourceEntity>> PagesAsync()
        {
            yield return new[] { new SourceEntity("present1", "{\"id\":\"present1\"}") };
            await Task.Yield();
            yield return new[] { new SourceEntity("present2", "{\"id\":\"present2\"}") };
        }
    }

    private sealed class CountingTargetWriter : ITargetEntityWriter
    {
        private int _created;
        public int Created => Volatile.Read(ref _created);

        public Task CreateAsync(EntityKind kind, SourceEntity entity, CancellationToken cancellationToken)
        {
            Assert.StartsWith($"{{\"id\":\"{entity.Identifier}\",", entity.RawJson);
            _ = Interlocked.Increment(ref _created);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(EntityKind kind, SourceEntity entity, CancellationToken cancellationToken) => throw new InvalidOperationException("Unexpected update.");

        public Task DeleteAsync(EntityKind kind, string identifier, CancellationToken cancellationToken) => throw new InvalidOperationException("Unexpected delete.");
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

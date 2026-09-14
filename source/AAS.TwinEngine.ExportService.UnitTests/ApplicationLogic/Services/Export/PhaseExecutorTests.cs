using AAS.TwinEngine.ExportService.ApplicationLogic.Services.Crud;
using AAS.TwinEngine.ExportService.ApplicationLogic.Services.Export;
using AAS.TwinEngine.ExportService.ApplicationLogic.Services.State;
using AAS.TwinEngine.ExportService.DomainModel;

using Microsoft.Extensions.Logging;

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

    private PhaseExecutor CreateSut() => new(_sourceReader, _targetWriter, _stateStore, _decisionMaker, _logger);

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
        Assert.Equal(1, result.Deleted);
        Assert.Equal(1, result.Skipped);
        Assert.Equal(0, result.Failed);
        Assert.False(result.HasFailures);

        await _targetWriter.Received(1).CreateAsync(kind, sourceCreate, Arg.Any<CancellationToken>());
        await _stateStore.Received(1).UpsertAsync(
            Arg.Is<ExportedEntity>(e => e.Kind == kind && e.Identifier == "id_create"),
            Arg.Any<CancellationToken>());

        await _targetWriter.Received(1).UpdateAsync(kind, sourceUpdate, Arg.Any<CancellationToken>());
        await _stateStore.Received(1).UpsertAsync(
            Arg.Is<ExportedEntity>(e => e.Kind == kind && e.Identifier == "id_update"),
            Arg.Any<CancellationToken>());

        await _targetWriter.Received(1).DeleteAsync(kind, "id_delete", Arg.Any<CancellationToken>());
        await _stateStore.Received(1).DeleteAsync(kind, "id_delete", Arg.Any<CancellationToken>());
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
}

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

/// <summary>
/// Regression coverage for targets that integrate their registry (for example BaSyx with
/// AASREGISTRYINTEGRATION enabled). Such a target creates the descriptor itself while handling
/// the exporter's shell/submodel create, so the exporter's own descriptor create is answered
/// with 409 even though the descriptor belongs to an entity it owns.
/// </summary>
public class PhaseExecutorDescriptorConflictTests
{
    private readonly ISourceEntityReader _sourceReader = Substitute.For<ISourceEntityReader>();
    private readonly ITargetEntityWriter _targetWriter = Substitute.For<ITargetEntityWriter>();
    private readonly IStateStore _stateStore = Substitute.For<IStateStore>();
    private readonly ICrudDecisionMaker _decisionMaker = Substitute.For<ICrudDecisionMaker>();
    private readonly ILogger<PhaseExecutor> _logger = Substitute.For<ILogger<PhaseExecutor>>();
    private readonly ExportServiceConfig _config = new();

    private PhaseExecutor CreateSut() =>
        new(_sourceReader, _targetWriter, _stateStore, _decisionMaker, Options.Create(_config), _logger);

    private SourceEntity ArrangeCreatePhase(EntityKind kind, string identifier)
    {
        var source = new SourceEntity(identifier, $"{{\"id\":\"{identifier}\"}}");
        _sourceReader.ReadAllAsync(kind, Arg.Any<CancellationToken>()).Returns(new[] { source });
        _stateStore.LoadAsync(kind, Arg.Any<CancellationToken>()).Returns(Array.Empty<ExportedEntity>());
        _decisionMaker.Decide(kind, Arg.Any<IReadOnlyList<SourceEntity>>(), Arg.Any<IReadOnlyList<ExportedEntity>>())
            .Returns(new[] { new CrudDecision(ExportOperation.Create, kind, identifier, source) });
        return source;
    }

    private void ArrangeTargetConflictOnCreate(EntityKind kind, SourceEntity source) =>
        _targetWriter.CreateAsync(kind, source, Arg.Any<CancellationToken>())
            .ThrowsAsync(new TargetEntityConflictException(
                $"Target POST for {kind} {source.Identifier} failed with status 409. Response: already exists"));

    [Theory]
    [InlineData(EntityKind.ShellDescriptor, EntityKind.Shell)]
    [InlineData(EntityKind.SubmodelDescriptor, EntityKind.Submodel)]
    public async Task ExecuteAsync_WhenDescriptorConflictsAndDescribedEntityIsManaged_ReconcilesDescriptorAndRecordsOwnership(
        EntityKind descriptorKind, EntityKind describedKind)
    {
        // Arrange: the exporter already owns the shell/submodel the descriptor describes.
        var identifier = "https://mm-software.com/ids/aas/000-001";
        var source = ArrangeCreatePhase(descriptorKind, identifier);
        ArrangeTargetConflictOnCreate(descriptorKind, source);
        _stateStore.GetAsync(describedKind, identifier, Arg.Any<CancellationToken>())
            .Returns(new ExportedEntity(describedKind, identifier, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "hash"));
        var sut = CreateSut();

        // Act
        var result = await sut.ExecuteAsync(descriptorKind, CancellationToken.None);

        // Assert: the stale target descriptor is reconciled with the source instead of failing.
        await _targetWriter.Received(1).UpdateAsync(descriptorKind, source, Arg.Any<CancellationToken>());
        await _stateStore.Received(1).UpsertAsync(
            Arg.Is<ExportedEntity>(e =>
                e.Kind == descriptorKind &&
                e.Identifier == identifier &&
                e.ContentHash == source.ContentHash),
            Arg.Any<CancellationToken>());
        Assert.Equal(0, result.Failed);
        Assert.False(result.HasFailures);
    }

    [Theory]
    [InlineData(EntityKind.ShellDescriptor)]
    [InlineData(EntityKind.SubmodelDescriptor)]
    public async Task ExecuteAsync_WhenDescriptorConflictsAndDescribedEntityIsUnmanaged_LeavesTargetUntouched(
        EntityKind descriptorKind)
    {
        // Arrange: no ownership record for the described entity, so the target descriptor
        // belongs to data this exporter does not manage.
        var identifier = "urn:target-owned:descriptor";
        var source = ArrangeCreatePhase(descriptorKind, identifier);
        ArrangeTargetConflictOnCreate(descriptorKind, source);
        _stateStore.GetAsync(Arg.Any<EntityKind>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ExportedEntity?)null);
        var sut = CreateSut();

        // Act
        var result = await sut.ExecuteAsync(descriptorKind, CancellationToken.None);

        // Assert
        await _targetWriter.DidNotReceive().UpdateAsync(Arg.Any<EntityKind>(), Arg.Any<SourceEntity>(), Arg.Any<CancellationToken>());
        await _stateStore.DidNotReceive().UpsertAsync(Arg.Any<ExportedEntity>(), Arg.Any<CancellationToken>());
        Assert.Equal(1, result.Failed);
        Assert.Equal(0, result.Created);
        Assert.Equal(0, result.Updated);
    }

    [Theory]
    [InlineData(EntityKind.Shell)]
    [InlineData(EntityKind.Submodel)]
    [InlineData(EntityKind.ConceptDescription)]
    public async Task ExecuteAsync_WhenNonDescriptorConflicts_LeavesTargetUntouched(EntityKind kind)
    {
        // Arrange: shells, submodels and concept descriptions are never adopted on conflict,
        // because nothing proves the existing target entity came from this exporter.
        var identifier = "urn:pre-existing:entity";
        var source = ArrangeCreatePhase(kind, identifier);
        ArrangeTargetConflictOnCreate(kind, source);
        var sut = CreateSut();

        // Act
        var result = await sut.ExecuteAsync(kind, CancellationToken.None);

        // Assert
        await _targetWriter.DidNotReceive().UpdateAsync(Arg.Any<EntityKind>(), Arg.Any<SourceEntity>(), Arg.Any<CancellationToken>());
        await _stateStore.DidNotReceive().UpsertAsync(Arg.Any<ExportedEntity>(), Arg.Any<CancellationToken>());
        await _stateStore.DidNotReceive().GetAsync(Arg.Any<EntityKind>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        Assert.Equal(1, result.Failed);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDescriptorConflictIsReconciled_CountsAsUpdatedRatherThanCreated()
    {
        // Arrange
        var identifier = "https://mm-software.com/ids/aas/001-004";
        var source = ArrangeCreatePhase(EntityKind.ShellDescriptor, identifier);
        ArrangeTargetConflictOnCreate(EntityKind.ShellDescriptor, source);
        _stateStore.GetAsync(EntityKind.Shell, identifier, Arg.Any<CancellationToken>())
            .Returns(new ExportedEntity(EntityKind.Shell, identifier, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "hash"));
        var sut = CreateSut();

        // Act
        var result = await sut.ExecuteAsync(EntityKind.ShellDescriptor, CancellationToken.None);

        // Assert: nothing was created in the target, an existing descriptor was rewritten.
        Assert.Equal(0, result.Created);
        Assert.Equal(1, result.Updated);
        Assert.Equal(0, result.Failed);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDescriptorCreateSucceeds_DoesNotConsultDescribedEntityOwnership()
    {
        // Arrange: a target without registry integration accepts the descriptor create.
        var identifier = "urn:new:descriptor";
        var source = ArrangeCreatePhase(EntityKind.SubmodelDescriptor, identifier);
        var sut = CreateSut();

        // Act
        var result = await sut.ExecuteAsync(EntityKind.SubmodelDescriptor, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.Created);
        Assert.Equal(0, result.Updated);
        await _targetWriter.DidNotReceive().UpdateAsync(Arg.Any<EntityKind>(), Arg.Any<SourceEntity>(), Arg.Any<CancellationToken>());
        await _stateStore.DidNotReceive().GetAsync(Arg.Any<EntityKind>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _stateStore.Received(1).UpsertAsync(
            Arg.Is<ExportedEntity>(e => e.Kind == EntityKind.SubmodelDescriptor && e.Identifier == identifier),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenReconciledDescriptorIsExportedAgainUnchanged_SkipsWithoutFurtherWrites()
    {
        // Arrange: the run after reconciliation finds a matching hash, so the phase is idempotent.
        var identifier = "https://mm-software.com/ids/aas/000-001";
        var source = new SourceEntity(identifier, $"{{\"id\":\"{identifier}\"}}");
        var managed = new ExportedEntity(
            EntityKind.ShellDescriptor, identifier, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, source.ContentHash);

        _sourceReader.ReadAllAsync(EntityKind.ShellDescriptor, Arg.Any<CancellationToken>()).Returns(new[] { source });
        _stateStore.LoadAsync(EntityKind.ShellDescriptor, Arg.Any<CancellationToken>()).Returns(new[] { managed });
        _decisionMaker.Decide(EntityKind.ShellDescriptor, Arg.Any<IReadOnlyList<SourceEntity>>(), Arg.Any<IReadOnlyList<ExportedEntity>>())
            .Returns(new[] { new CrudDecision(ExportOperation.Skip, EntityKind.ShellDescriptor, identifier, source) });
        var sut = CreateSut();

        // Act
        var result = await sut.ExecuteAsync(EntityKind.ShellDescriptor, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.Skipped);
        Assert.Equal(0, result.Failed);
        await _targetWriter.DidNotReceive().CreateAsync(Arg.Any<EntityKind>(), Arg.Any<SourceEntity>(), Arg.Any<CancellationToken>());
        await _targetWriter.DidNotReceive().UpdateAsync(Arg.Any<EntityKind>(), Arg.Any<SourceEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenReconciliationUpdateFails_DoesNotRecordOwnership()
    {
        // Arrange: a failed reconciliation must not claim ownership of the target descriptor.
        var identifier = "urn:descriptor:update-fails";
        var source = ArrangeCreatePhase(EntityKind.ShellDescriptor, identifier);
        ArrangeTargetConflictOnCreate(EntityKind.ShellDescriptor, source);
        _stateStore.GetAsync(EntityKind.Shell, identifier, Arg.Any<CancellationToken>())
            .Returns(new ExportedEntity(EntityKind.Shell, identifier, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "hash"));
        _targetWriter.UpdateAsync(EntityKind.ShellDescriptor, source, Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Target PUT failed with status 400."));
        var sut = CreateSut();

        // Act
        var result = await sut.ExecuteAsync(EntityKind.ShellDescriptor, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.Failed);
        await _stateStore.DidNotReceive().UpsertAsync(Arg.Any<ExportedEntity>(), Arg.Any<CancellationToken>());
    }
}

using AAS.TwinEngine.ExportService.ApplicationLogic.Exceptions;
using AAS.TwinEngine.ExportService.ApplicationLogic.Services.Export;
using AAS.TwinEngine.ExportService.DomainModel;
using AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace AAS.TwinEngine.ExportService.UnitTests.ApplicationLogic.Services.Export;

public class ExportRunnerTests
{
    private readonly IPhaseExecutor _phaseExecutor = Substitute.For<IPhaseExecutor>();
    private readonly IOptionsMonitor<ExportServiceConfig> _config = Substitute.For<IOptionsMonitor<ExportServiceConfig>>();
    private readonly ILogger<ExportRunner> _logger = Substitute.For<ILogger<ExportRunner>>();
    private readonly ExportServiceConfig _configValue = new();

    public ExportRunnerTests()
    {
        _config.CurrentValue.Returns(_configValue);

        // Default: all phases succeed with 0 failures
        _phaseExecutor.ExecuteAsync(Arg.Any<EntityKind>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var kind = callInfo.Arg<EntityKind>();
                return Task.FromResult(new PhaseResult(kind, 1, 0, 0, 0, 0));
            });
    }

    private ExportRunner CreateSut() => new(_phaseExecutor, _config, _logger);

    [Fact]
    public async Task RunAsync_WhenAllPhasesEnabledAndSucceed_ReturnsSuccessResultWithAllPhases()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.RunAsync(CancellationToken.None);

        // Assert
        Assert.Equal(RunStatus.Success, result.Status);
        Assert.Equal(5, result.Phases.Count);
        Assert.True(result.FinishedAt >= result.StartedAt);

        await _phaseExecutor.Received(1).ExecuteAsync(EntityKind.ConceptDescription, Arg.Any<CancellationToken>());
        await _phaseExecutor.Received(1).ExecuteAsync(EntityKind.Submodel, Arg.Any<CancellationToken>());
        await _phaseExecutor.Received(1).ExecuteAsync(EntityKind.SubmodelDescriptor, Arg.Any<CancellationToken>());
        await _phaseExecutor.Received(1).ExecuteAsync(EntityKind.Shell, Arg.Any<CancellationToken>());
        await _phaseExecutor.Received(1).ExecuteAsync(EntityKind.ShellDescriptor, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_ExecutesPhasesInStrictDependencyOrder()
    {
        // Arrange
        var sut = CreateSut();
        var executedKinds = new List<EntityKind>();

        _phaseExecutor.ExecuteAsync(Arg.Any<EntityKind>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var kind = callInfo.Arg<EntityKind>();
                executedKinds.Add(kind);
                return Task.FromResult(new PhaseResult(kind, 1, 0, 0, 0, 0));
            });

        // Act
        var result = await sut.RunAsync(CancellationToken.None);

        // Assert
        Assert.Equal(
            new[]
            {
                EntityKind.ConceptDescription,
                EntityKind.Submodel,
                EntityKind.SubmodelDescriptor,
                EntityKind.Shell,
                EntityKind.ShellDescriptor
            },
            executedKinds);
    }

    [Fact]
    public async Task RunAsync_WhenPhaseHasFailures_SetsStatusToPartialFailureAndContinuesRemainingPhases()
    {
        // Arrange
        var sut = CreateSut();
        _phaseExecutor.ExecuteAsync(EntityKind.Submodel, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new PhaseResult(EntityKind.Submodel, 1, 0, 0, 0, 1))); // failed = 1

        // Act
        var result = await sut.RunAsync(CancellationToken.None);

        // Assert
        Assert.Equal(RunStatus.PartialFailure, result.Status);
        Assert.Equal(5, result.Phases.Count);
        await _phaseExecutor.Received(1).ExecuteAsync(EntityKind.Shell, Arg.Any<CancellationToken>());
        await _phaseExecutor.Received(1).ExecuteAsync(EntityKind.ShellDescriptor, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WhenSourceUnavailableExceptionThrown_AbortsRemainingPhases()
    {
        // Arrange
        var sut = CreateSut();
        _phaseExecutor.ExecuteAsync(EntityKind.Submodel, Arg.Any<CancellationToken>())
            .Returns<Task<PhaseResult>>(_ => throw new SourceUnavailableException("Source down"));

        // Act
        var result = await sut.RunAsync(CancellationToken.None);

        // Assert
        Assert.Equal(RunStatus.Aborted, result.Status);
        Assert.Single(result.Phases); // Only ConceptDescription executed
        Assert.Equal(EntityKind.ConceptDescription, result.Phases[0].Kind);

        await _phaseExecutor.DidNotReceive().ExecuteAsync(EntityKind.SubmodelDescriptor, Arg.Any<CancellationToken>());
        await _phaseExecutor.DidNotReceive().ExecuteAsync(EntityKind.Shell, Arg.Any<CancellationToken>());
        await _phaseExecutor.DidNotReceive().ExecuteAsync(EntityKind.ShellDescriptor, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WhenUnexpectedExceptionThrown_AbortsRemainingPhases()
    {
        // Arrange
        var sut = CreateSut();
        _phaseExecutor.ExecuteAsync(EntityKind.ConceptDescription, Arg.Any<CancellationToken>())
            .Returns<Task<PhaseResult>>(_ => throw new InvalidOperationException("Unexpected error"));

        // Act
        var result = await sut.RunAsync(CancellationToken.None);

        // Assert
        Assert.Equal(RunStatus.Aborted, result.Status);
        Assert.Empty(result.Phases);

        await _phaseExecutor.DidNotReceive().ExecuteAsync(EntityKind.Submodel, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WhenOperationCanceledExceptionThrown_RethrowsWithPhaseInfo()
    {
        // Arrange
        var sut = CreateSut();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _phaseExecutor.ExecuteAsync(EntityKind.Submodel, Arg.Any<CancellationToken>())
            .Returns<Task<PhaseResult>>(_ => throw new OperationCanceledException(cts.Token));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<OperationCanceledException>(() => sut.RunAsync(CancellationToken.None));
        Assert.Contains("Submodel", ex.Message);
    }

    [Fact]
    public async Task RunAsync_WhenPreCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var sut = CreateSut();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.RunAsync(cts.Token));
        await _phaseExecutor.DidNotReceive().ExecuteAsync(Arg.Any<EntityKind>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(EntityKind.ConceptDescription, false, true)]
    [InlineData(EntityKind.ConceptDescription, true, false)]
    [InlineData(EntityKind.Submodel, false, true)]
    [InlineData(EntityKind.Submodel, true, false)]
    [InlineData(EntityKind.SubmodelDescriptor, false, true)]
    [InlineData(EntityKind.SubmodelDescriptor, true, false)]
    [InlineData(EntityKind.Shell, false, true)]
    [InlineData(EntityKind.Shell, true, false)]
    [InlineData(EntityKind.ShellDescriptor, false, true)]
    [InlineData(EntityKind.ShellDescriptor, true, false)]
    public async Task RunAsync_WhenPhaseDisabledInSourceOrTarget_SkipsThatPhase(
        EntityKind kindToDisable, bool sourceEnabled, bool targetEnabled)
    {
        // Arrange
        SetPhaseEnabled(kindToDisable, sourceEnabled, targetEnabled);
        var sut = CreateSut();

        // Act
        var result = await sut.RunAsync(CancellationToken.None);

        // Assert
        Assert.Equal(RunStatus.Success, result.Status);
        Assert.Equal(4, result.Phases.Count);
        Assert.DoesNotContain(result.Phases, p => p.Kind == kindToDisable);
        await _phaseExecutor.DidNotReceive().ExecuteAsync(kindToDisable, Arg.Any<CancellationToken>());
    }

    private void SetPhaseEnabled(EntityKind kind, bool sourceEnabled, bool targetEnabled)
    {
        switch (kind)
        {
            case EntityKind.ConceptDescription:
                _configValue.Sources.ConceptDescriptions.Enabled = sourceEnabled;
                _configValue.Targets.ConceptDescriptions.Enabled = targetEnabled;
                break;
            case EntityKind.Submodel:
                _configValue.Sources.Submodels.Enabled = sourceEnabled;
                _configValue.Targets.Submodels.Enabled = targetEnabled;
                break;
            case EntityKind.SubmodelDescriptor:
                _configValue.Sources.SubmodelDescriptors.Enabled = sourceEnabled;
                _configValue.Targets.SubmodelDescriptors.Enabled = targetEnabled;
                break;
            case EntityKind.Shell:
                _configValue.Sources.Shells.Enabled = sourceEnabled;
                _configValue.Targets.Shells.Enabled = targetEnabled;
                break;
            case EntityKind.ShellDescriptor:
                _configValue.Sources.ShellDescriptors.Enabled = sourceEnabled;
                _configValue.Targets.ShellDescriptors.Enabled = targetEnabled;
                break;
        }
    }
}

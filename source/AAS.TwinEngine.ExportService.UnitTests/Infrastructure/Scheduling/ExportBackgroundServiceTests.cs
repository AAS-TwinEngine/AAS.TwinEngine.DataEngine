using System.Reflection;

using AAS.TwinEngine.ExportService.ApplicationLogic.Services.Export;
using AAS.TwinEngine.ExportService.DomainModel;
using AAS.TwinEngine.ExportService.Infrastructure.Scheduling;
using AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace AAS.TwinEngine.ExportService.UnitTests.Infrastructure.Scheduling;

public class ExportBackgroundServiceTests
{
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();
    private readonly IExportRunLock _runLock = Substitute.For<IExportRunLock>();
    private readonly IOptionsMonitor<ExportServiceConfig> _config = Substitute.For<IOptionsMonitor<ExportServiceConfig>>();
    private readonly ILogger<ExportBackgroundService> _logger = Substitute.For<ILogger<ExportBackgroundService>>();
    private readonly ExportServiceConfig _configValue = new();

    public ExportBackgroundServiceTests()
    {
        _config.CurrentValue.Returns(_configValue);
        _configValue.Scheduler.Enabled = true;
        _configValue.Scheduler.CronExpression = "0 * * * *";
        _configValue.Scheduler.RunTimeoutMinutes = 30;
    }

    private ExportBackgroundService CreateSut() => new(_serviceProvider, _runLock, _config, _logger);

    private static async Task InvokeTickAsync(ExportBackgroundService sut, CancellationToken stoppingToken)
    {
        var method = typeof(ExportBackgroundService).GetMethod(
            "TickAsync",
            BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("TickAsync method not found");

        var task = (Task)method.Invoke(sut, new object[] { stoppingToken })!;
        await task;
    }

    private static async Task InvokeExecuteAsync(ExportBackgroundService sut, CancellationToken stoppingToken)
    {
        var method = typeof(ExportBackgroundService).GetMethod(
            "ExecuteAsync",
            BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("ExecuteAsync method not found");

        var task = (Task)method.Invoke(sut, new object[] { stoppingToken })!;
        await task;
    }

    [Fact]
    public async Task ExecuteAsync_WhenSchedulerDisabled_ExitsImmediately()
    {
        // Arrange
        _configValue.Scheduler.Enabled = false;
        var sut = CreateSut();

        // Act
        await InvokeExecuteAsync(sut, CancellationToken.None);

        // Assert
        _runLock.DidNotReceive().TryAcquire();
    }

    [Fact]
    public async Task ExecuteAsync_WhenInvalidCronExpression_LogsCriticalAndExits()
    {
        // Arrange
        _configValue.Scheduler.CronExpression = "invalid-cron-string";
        var sut = CreateSut();

        // Act
        await InvokeExecuteAsync(sut, CancellationToken.None);

        // Assert
        _runLock.DidNotReceive().TryAcquire();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancelledDuringDelay_StopsGracefully()
    {
        // Arrange
        _configValue.Scheduler.CronExpression = "0 * * * *"; // Next occurrence within 1 hour (< 49 days)
        var sut = CreateSut();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(20));

        // Act
        await InvokeExecuteAsync(sut, cts.Token);

        // Assert
        _runLock.DidNotReceive().TryAcquire();
    }

    [Fact]
    public async Task TickAsync_WhenLockCannotBeAcquired_LogsWarningAndDoesNotRun()
    {
        // Arrange
        _runLock.TryAcquire().Returns((IDisposable?)null);
        var sut = CreateSut();

        // Act
        await InvokeTickAsync(sut, CancellationToken.None);

        // Assert
        _serviceProvider.DidNotReceive().GetService(Arg.Any<Type>());
    }

    [Fact]
    public async Task TickAsync_WhenLockAcquired_ExecutesRunnerInsideScope()
    {
        // Arrange
        var releaser = Substitute.For<IDisposable>();
        _runLock.TryAcquire().Returns(releaser);

        var runner = Substitute.For<IExportRunner>();
        runner.RunAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ExportRunResult(
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                RunStatus.Success,
                Array.Empty<PhaseResult>())));

        var scope = Substitute.For<IServiceScope>();
        var scopedProvider = Substitute.For<IServiceProvider>();
        scopedProvider.GetService(typeof(IExportRunner)).Returns(runner);
        scope.ServiceProvider.Returns(scopedProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);
        _serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);

        var sut = CreateSut();

        // Act
        await InvokeTickAsync(sut, CancellationToken.None);

        // Assert
        await runner.Received(1).RunAsync(Arg.Any<CancellationToken>());
        releaser.Received(1).Dispose();
        scope.Received(1).Dispose();
    }

    [Fact]
    public async Task TickAsync_WhenRunnerTimesOut_LogsCriticalAndHandlesTimeoutGracefully()
    {
        // Arrange
        var releaser = Substitute.For<IDisposable>();
        _runLock.TryAcquire().Returns(releaser);
        _configValue.Scheduler.RunTimeoutMinutes = 0; // Immediate timeout

        var runner = Substitute.For<IExportRunner>();
        runner.RunAsync(Arg.Any<CancellationToken>())
            .Returns<Task<ExportRunResult>>(async callInfo =>
            {
                var ct = callInfo.Arg<CancellationToken>();
                await Task.Delay(100, ct);
                return new ExportRunResult(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, RunStatus.Success, Array.Empty<PhaseResult>());
            });

        var scope = Substitute.For<IServiceScope>();
        var scopedProvider = Substitute.For<IServiceProvider>();
        scopedProvider.GetService(typeof(IExportRunner)).Returns(runner);
        scope.ServiceProvider.Returns(scopedProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);
        _serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);

        var sut = CreateSut();

        // Act & Assert (should not throw unhandled exception)
        await InvokeTickAsync(sut, CancellationToken.None);
        releaser.Received(1).Dispose();
    }

    [Fact]
    public async Task TickAsync_WhenRunnerThrowsUnexpectedException_LogsErrorAndDoesNotCrash()
    {
        // Arrange
        var releaser = Substitute.For<IDisposable>();
        _runLock.TryAcquire().Returns(releaser);

        var runner = Substitute.For<IExportRunner>();
        runner.RunAsync(Arg.Any<CancellationToken>())
            .Returns<Task<ExportRunResult>>(_ => throw new InvalidOperationException("Export pipeline failed"));

        var scope = Substitute.For<IServiceScope>();
        var scopedProvider = Substitute.For<IServiceProvider>();
        scopedProvider.GetService(typeof(IExportRunner)).Returns(runner);
        scope.ServiceProvider.Returns(scopedProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);
        _serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);

        var sut = CreateSut();

        // Act & Assert (must catch exception so worker service does not crash)
        await InvokeTickAsync(sut, CancellationToken.None);
        releaser.Received(1).Dispose();
    }

    [Fact]
    public async Task TickAsync_WhenHostStopping_HandlesCancellationCleanly()
    {
        // Arrange
        var releaser = Substitute.For<IDisposable>();
        _runLock.TryAcquire().Returns(releaser);

        using var stoppingCts = new CancellationTokenSource();
        stoppingCts.Cancel();

        var runner = Substitute.For<IExportRunner>();
        runner.RunAsync(Arg.Any<CancellationToken>())
            .Returns<Task<ExportRunResult>>(_ => throw new OperationCanceledException(stoppingCts.Token));

        var scope = Substitute.For<IServiceScope>();
        var scopedProvider = Substitute.For<IServiceProvider>();
        scopedProvider.GetService(typeof(IExportRunner)).Returns(runner);
        scope.ServiceProvider.Returns(scopedProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);
        _serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);

        var sut = CreateSut();

        // Act
        await InvokeTickAsync(sut, stoppingCts.Token);

        // Assert
        releaser.Received(1).Dispose();
    }
}

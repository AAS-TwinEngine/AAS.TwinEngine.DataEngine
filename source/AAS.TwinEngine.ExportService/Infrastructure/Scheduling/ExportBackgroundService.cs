using AAS.TwinEngine.ExportService.ApplicationLogic.Services.Export;
using AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

using Cronos;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.ExportService.Infrastructure.Scheduling;

/// <summary>
/// BackgroundService that ticks on a cron schedule and runs the export pipeline.
/// Skips ticks with a warning if a previous run is still in progress. Cancels a run if it
/// exceeds the configured timeout.
/// </summary>
public sealed class ExportBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IExportRunLock _runLock;
    private readonly IOptionsMonitor<ExportServiceConfig> _config;
    private readonly ILogger<ExportBackgroundService> _logger;

    public ExportBackgroundService(
        IServiceProvider serviceProvider,
        IExportRunLock runLock,
        IOptionsMonitor<ExportServiceConfig> config,
        ILogger<ExportBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _runLock = runLock;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var scheduler = _config.CurrentValue.Scheduler;

        if (!scheduler.Enabled)
        {
            _logger.LogWarning("Export scheduler is disabled by configuration. No runs will be executed.");
            return;
        }

        CronExpression cron;
        try
        {
            cron = CronExpression.Parse(scheduler.CronExpression);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(
                ex,
                "Invalid cron expression '{Cron}'. Export scheduler cannot start.",
                scheduler.CronExpression);
            return;
        }

        _logger.LogInformation(
            "Export scheduler started. Cron='{Cron}', RunTimeout={TimeoutMinutes}min.",
            scheduler.CronExpression, scheduler.RunTimeoutMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            var next = cron.GetNextOccurrence(DateTimeOffset.UtcNow, TimeZoneInfo.Utc);
            if (next is null)
            {
                _logger.LogWarning("Cron expression produced no next occurrence. Scheduler is exiting.");
                return;
            }

            var delay = next.Value - DateTimeOffset.UtcNow;
            if (delay > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }

            await TickAsync(stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task TickAsync(CancellationToken stoppingToken)
    {
        using var acquired = _runLock.TryAcquire();
        if (acquired is null)
        {
            _logger.LogError(
                "Skipping scheduled export tick: a previous run is still in progress.");
            return;
        }

        var timeout = TimeSpan.FromMinutes(_config.CurrentValue.Scheduler.RunTimeoutMinutes);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        timeoutCts.CancelAfter(timeout);

        using var scope = _serviceProvider.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IExportRunner>();

        try
        {
            _ = await runner.RunAsync(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (timeoutCts.IsCancellationRequested && !stoppingToken.IsCancellationRequested)
        {
            _logger.LogCritical(
                ex,
                "Export run exceeded configured timeout of {TimeoutMinutes} minutes and was cancelled.",
                _config.CurrentValue.Scheduler.RunTimeoutMinutes);
        }
        catch (OperationCanceledException)
        {
            // Host is stopping — nothing else to log.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export run failed with an unhandled exception.");
        }
    }
}

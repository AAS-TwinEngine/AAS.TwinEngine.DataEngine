using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRegistry;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.AasRegistryProvider.Config;

using Cronos;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.AasRegistryProvider.Services;

public class ShellDescriptorSyncHosted(
    IServiceScopeFactory scopeFactory,
    IOptions<AasRegistryPreComputed> config,
    ILogger<ShellDescriptorSyncHosted> logger) : BackgroundService
{
    private readonly CronExpression _cronExpression = CronExpression.Parse(config.Value.ShellDescriptorCron, CronFormat.IncludeSeconds);
    private readonly bool _isPreComputed = config.Value.IsPreComputed;
    private readonly TimeZoneInfo _timeZone = TimeZoneInfo.Local;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ShellDescriptor Cron sync started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var next = _cronExpression.GetNextOccurrence(DateTimeOffset.UtcNow, _timeZone);
            if (!next.HasValue)
            {
                logger.LogWarning("Cron expression returned no next occurrence.");
                break;
            }

            var delay = next.Value - DateTimeOffset.UtcNow;
            if (delay > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
                }
                catch (TaskCanceledException ex)
                {
                    logger.LogInformation(ex, "Task delay was canceled. Stopping execution loop.");
                    break;
                }
            }

            try
            {
                await RunOnceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (HttpRequestException)
            {
                throw new ResourceNotFoundException();
            }
            catch (TaskCanceledException)
            {
                throw new RequestTimeoutException();
            }
        }
    }

    public async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        if (!_isPreComputed)
        {
            logger.LogInformation("Skipping ShellDescriptor sync as Pre-Computed set to false.");
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var syncService = scope.ServiceProvider.GetRequiredService<IShellDescriptorService>();

        await syncService.SyncShellDescriptorsAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("ShellDescriptor sync completed at {Time}", DateTimeOffset.UtcNow);
    }
}

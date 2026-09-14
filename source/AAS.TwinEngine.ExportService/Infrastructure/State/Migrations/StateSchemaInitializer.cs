using AAS.TwinEngine.ExportService.ApplicationLogic.Services.State;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AAS.TwinEngine.ExportService.Infrastructure.State.Migrations;

/// <summary>
/// Runs once at application startup to make sure the state schema exists. Idempotent —
/// uses CREATE ... IF NOT EXISTS statements so it is safe to run on every start.
/// </summary>
public sealed class StateSchemaInitializer : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StateSchemaInitializer> _logger;

    public StateSchemaInitializer(IServiceProvider serviceProvider, ILogger<StateSchemaInitializer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing state store schema...");
        using var scope = _serviceProvider.CreateScope();
        var stateStore = scope.ServiceProvider.GetRequiredService<IStateStore>();
        await stateStore.EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

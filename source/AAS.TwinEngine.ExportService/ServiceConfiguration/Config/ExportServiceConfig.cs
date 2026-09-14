namespace AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

/// <summary>
/// Root configuration section for the Export Service.
/// </summary>
public sealed class ExportServiceConfig
{
    public const string Section = "ExportService";

    public SchedulerConfig Scheduler { get; set; } = new();

    public ResilienceConfig Resilience { get; set; } = new();

    public StateStoreConfig StateStore { get; set; } = new();

    public SourceEndpointsConfig Sources { get; set; } = new();

    public TargetEndpointsConfig Targets { get; set; } = new();

    public ObservabilityConfig Observability { get; set; } = new();
}

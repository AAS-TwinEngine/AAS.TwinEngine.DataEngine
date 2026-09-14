namespace AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

/// <summary>
/// Configuration for the periodic export scheduler.
/// </summary>
public sealed class SchedulerConfig
{
    /// <summary>
    /// When <c>false</c>, no scheduled runs are executed. Default: <c>true</c>.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Cron expression (Cronos format, 5 fields). Example: "0 * * * *" (every hour).
    /// </summary>
    public string CronExpression { get; set; } = "0 * * * *";

    /// <summary>
    /// Maximum allowed duration for a single export cycle before cancellation.
    /// </summary>
    public int RunTimeoutMinutes { get; set; } = 30;
}

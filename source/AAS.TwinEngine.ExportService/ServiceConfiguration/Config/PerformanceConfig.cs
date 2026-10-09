namespace AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

/// <summary>
/// Tuning knobs for how aggressively a phase applies changes to the target.
/// </summary>
public sealed class PerformanceConfig
{
    /// <summary>
    /// Maximum number of entities applied to the target concurrently within a single phase
    /// (create/update/delete-verify/delete). Set to <c>1</c> for fully sequential behavior.
    /// Higher values reduce wall-clock time for large datasets at the cost of more concurrent
    /// HTTP calls and database connections. Default: <c>8</c>.
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = 8;
}

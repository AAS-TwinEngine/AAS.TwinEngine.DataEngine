namespace AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

/// <summary>
/// HTTP retry policy applied to all outgoing calls.
/// </summary>
public sealed class ResilienceConfig
{
    public int MaxRetryAttempts { get; set; } = 3;

    public int InitialDelaySeconds { get; set; } = 2;

    public double BackoffMultiplier { get; set; } = 2.0;
}

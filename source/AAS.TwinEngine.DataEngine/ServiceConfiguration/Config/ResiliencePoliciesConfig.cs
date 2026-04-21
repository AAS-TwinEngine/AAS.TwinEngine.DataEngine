namespace AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

public class ResiliencePoliciesConfig
{
    public RetryConfig Retry { get; set; } = new();
}

public class RetryConfig
{
    public int MaxRetryAttempts { get; set; }
    public int DelayInSeconds { get; set; }
    public double BackoffMultiplier { get; set; } = 2.0;
}

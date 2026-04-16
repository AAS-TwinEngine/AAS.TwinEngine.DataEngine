namespace AAS.TwinEngine.DataEngine.ServiceConfiguration.Config.Helpers;

public class ResiliencePoliciesConfig
{
    public RetryConfig Retry { get; set; } = new();
}

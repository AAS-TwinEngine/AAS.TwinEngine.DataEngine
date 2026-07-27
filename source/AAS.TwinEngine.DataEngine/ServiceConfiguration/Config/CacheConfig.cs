namespace AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

public class CacheConfig
{
    public const string Section = "General:Cache";

    public bool EnableNoCacheParameter { get; set; } = false;
}

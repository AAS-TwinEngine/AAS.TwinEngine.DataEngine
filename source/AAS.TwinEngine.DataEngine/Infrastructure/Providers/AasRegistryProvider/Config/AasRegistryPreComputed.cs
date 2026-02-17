namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.AasRegistryProvider.Config;

public class AasRegistryPreComputed
{
    public const string Section = "AasRegistryPreComputed";

    public string ShellDescriptorCron { get; set; } = "0 */3 * * * *";

    public bool IsPreComputed { get; set; } = false;
}

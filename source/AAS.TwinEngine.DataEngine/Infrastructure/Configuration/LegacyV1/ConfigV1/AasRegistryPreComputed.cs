namespace AAS.TwinEngine.DataEngine.Infrastructure.Configuration.LegacyV1;

public class AasRegistryPreComputed
{
    public const string Section = "AasRegistryPreComputed";

    public string ShellDescriptorCron { get; set; } = "0 */3 * * * *";

    public bool IsPreComputed { get; set; } = false;
}

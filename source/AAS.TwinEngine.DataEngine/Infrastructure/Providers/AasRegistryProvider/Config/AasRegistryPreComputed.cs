namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.AasRegistryProvider.Config;

public class AasRegistryPreComputed
{
    public const string Section = "AasRegistryPreComputed";

    public required string ShellDescriptorCron { get; set; }

    public required bool IsPreComputed { get; set; }
}

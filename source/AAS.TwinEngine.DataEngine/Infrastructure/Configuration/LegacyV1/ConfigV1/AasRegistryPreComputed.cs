namespace AAS.TwinEngine.DataEngine.Infrastructure.Configuration.LegacyV1;

#pragma warning disable S1133 
[Obsolete("V1 configuration is deprecated and will be removed in next major release")]
public class AasRegistryPreComputed
{
    public const string Section = "AasRegistryPreComputed";

    public string ShellDescriptorCron { get; set; } = "0 */3 * * * *";

    public bool IsPreComputed { get; set; } = false;
}

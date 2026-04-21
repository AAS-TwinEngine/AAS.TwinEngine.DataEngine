namespace AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

/// <summary>
/// V2 config — binds to the "RegistrySettings" section.
/// </summary>
public class RegistrySettingsConfig
{
    public const string Section = "RegistrySettings";

    public PreComputedConfig PreComputed { get; set; } = new();
}

public class PreComputedConfig
{
    public bool Enabled { get; set; } = false;
    public string Schedule { get; set; } = "0 */3 * * * *";
}

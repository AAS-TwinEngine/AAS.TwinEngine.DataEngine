namespace AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

/// <summary>
/// V2 config — binds to the "RegistrySettings" section.
/// Note: The V2 JSON has a typo "ResgistrySettings" — both spellings are handled by the normalizer.
/// </summary>
public class RegistrySettingsConfig
{
    public const string Section = "RegistrySettings";

    /// <summary>
    /// Also check for typo variant in new JSON: "ResgistrySettings".
    /// </summary>
    public const string SectionTypoVariant = "ResgistrySettings";

    public PreComputedConfig PreComputed { get; set; } = new();
}

public class PreComputedConfig
{
    public bool Enabled { get; set; } = false;
    public string Schedule { get; set; } = "0 */3 * * * *";
}

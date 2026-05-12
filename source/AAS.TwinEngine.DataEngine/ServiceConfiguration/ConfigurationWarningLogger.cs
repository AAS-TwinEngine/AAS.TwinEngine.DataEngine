namespace AAS.TwinEngine.DataEngine.ServiceConfiguration;

public static class ConfigurationWarningLogger
{
    public const string PreComputedSection = "AasRegistryPreComputed";

    public static void LogDeprecatedSections(IConfiguration configuration, ILogger logger)
    {
        if (!configuration.GetSection(PreComputedSection).Exists())
        {
            return;
        }

        logger.LogWarning(
            "Detected deprecated configuration section '{SectionName}'. The precomputed flow has been removed and this section is ignored. Please remove it from your configuration.",
            PreComputedSection);
    }
}

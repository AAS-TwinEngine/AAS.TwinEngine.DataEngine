using Cronos;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

public class RegistrySettingsConfigValidator : IValidateOptions<RegistrySettingsConfig>
{
    public ValidateOptionsResult Validate(string? name, RegistrySettingsConfig options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.PreComputed.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        var schedule = options.PreComputed.Schedule;
        if (string.IsNullOrWhiteSpace(schedule))
        {
            return ValidateOptionsResult.Fail(
                $"{RegistrySettingsConfig.Section}.PreComputed.Schedule is required when PreComputed.Enabled is true.");
        }

        try
        {
            CronExpression.Parse(schedule, CronFormat.IncludeSeconds);
        }
        catch (CronFormatException ex)
        {
            return ValidateOptionsResult.Fail(
                $"{RegistrySettingsConfig.Section}.PreComputed.Schedule is not a valid cron expression: '{schedule}'. {ex.Message}");
        }

        return ValidateOptionsResult.Success;
    }
}

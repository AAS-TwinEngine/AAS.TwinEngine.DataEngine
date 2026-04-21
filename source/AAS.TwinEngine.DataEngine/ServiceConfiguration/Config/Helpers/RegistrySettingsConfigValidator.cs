using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

using Cronos;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.ServiceConfiguration.Config.Helpers;

public class RegistrySettingsConfigValidator : IValidateOptions<RegistrySettingsConfig>
{
    public ValidateOptionsResult Validate(string? name, RegistrySettingsConfig options)
    {
        if (options is null)
        {
            throw new InvalidDependencyException(nameof(options));
        }

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
            _ = CronExpression.Parse(schedule, CronFormat.IncludeSeconds);
        }
        catch (CronFormatException ex)
        {
            return ValidateOptionsResult.Fail(
                $"{RegistrySettingsConfig.Section}.PreComputed.Schedule is not a valid cron expression: '{schedule}'. {ex.Message}");
        }

        return ValidateOptionsResult.Success;
    }
}

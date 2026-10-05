 using Cronos;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

public sealed class ExportServiceConfigValidator : IValidateOptions<ExportServiceConfig>
{
    public ValidateOptionsResult Validate(string? name, ExportServiceConfig options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        ValidateScheduler(options.Scheduler, failures);

        if (string.IsNullOrWhiteSpace(options.StateStore.ConnectionString))
        {
            failures.Add("StateStore.ConnectionString must be configured.");
        }

        ValidateEndpoints("Sources", options.Sources, failures, validateLimit: true);
        ValidateEndpoints("Targets", options.Targets, failures);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateScheduler(SchedulerConfig scheduler, List<string> failures)
    {
        if (scheduler.RunTimeoutMinutes <= 0)
        {
            failures.Add("Scheduler.RunTimeoutMinutes must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(scheduler.CronExpression))
        {
            failures.Add("Scheduler.CronExpression must be configured.");
            return;
        }

        try
        {
            _ = CronExpression.Parse(scheduler.CronExpression, CronFormat.Standard);
        }
        catch (FormatException)
        {
            failures.Add("Scheduler.CronExpression must be a valid five-field cron expression.");
        }
    }

    private static void ValidateEndpoints(
        string groupName,
        SourceEndpointsConfig endpoints,
        List<string> failures,
        bool validateLimit)
    {
        ValidateEndpoint($"{groupName}.ShellDescriptors", endpoints.ShellDescriptors, failures, validateLimit);
        ValidateEndpoint($"{groupName}.SubmodelDescriptors", endpoints.SubmodelDescriptors, failures, validateLimit);
        ValidateEndpoint($"{groupName}.Shells", endpoints.Shells, failures, validateLimit);
        ValidateEndpoint($"{groupName}.Submodels", endpoints.Submodels, failures, validateLimit);
        ValidateEndpoint($"{groupName}.ConceptDescriptions", endpoints.ConceptDescriptions, failures, validateLimit);
    }

    private static void ValidateEndpoints(string groupName, TargetEndpointsConfig endpoints, List<string> failures)
    {
        ValidateEndpoint($"{groupName}.ShellDescriptors", endpoints.ShellDescriptors, failures, validateLimit: false);
        ValidateEndpoint($"{groupName}.SubmodelDescriptors", endpoints.SubmodelDescriptors, failures, validateLimit: false);
        ValidateEndpoint($"{groupName}.Shells", endpoints.Shells, failures, validateLimit: false);
        ValidateEndpoint($"{groupName}.Submodels", endpoints.Submodels, failures, validateLimit: false);
        ValidateEndpoint($"{groupName}.ConceptDescriptions", endpoints.ConceptDescriptions, failures, validateLimit: false);
    }

    private static void ValidateEndpoint(string name, EndpointConfig endpoint, List<string> failures, bool validateLimit)
    {
        if (!endpoint.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(endpoint.BaseUrl))
        {
            failures.Add($"{name}.BaseUrl must be configured when the endpoint is enabled.");
        }
        else if (!Uri.TryCreate(endpoint.BaseUrl, UriKind.Absolute, out _))
        {
            failures.Add($"{name}.BaseUrl must be an absolute URI when the endpoint is enabled.");
        }

        if (string.IsNullOrWhiteSpace(endpoint.Path))
        {
            failures.Add($"{name}.Path must be configured when the endpoint is enabled.");
        }

        if (validateLimit && endpoint.Limit <= 0)
        {
            failures.Add($"{name}.Limit must be greater than zero when the endpoint is enabled.");
        }
    }
}
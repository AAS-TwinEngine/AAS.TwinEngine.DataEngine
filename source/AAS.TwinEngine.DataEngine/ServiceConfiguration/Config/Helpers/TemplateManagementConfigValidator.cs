using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.ServiceConfiguration.Config.Helpers;

public class TemplateManagementConfigValidator : IValidateOptions<TemplateManagementConfig>
{
    private static readonly (string Name, Func<TemplateManagementConfig, ServiceInstance> Accessor)[] Endpoints =
    [
        ("AasTemplateRepository", c => c.AasTemplateRepository),
        ("SubmodelTemplateRepository", c => c.SubmodelTemplateRepository),
        ("ConceptDescriptionTemplateRepository", c => c.ConceptDescriptionTemplateRepository),
        ("AasTemplateRegistry", c => c.AasTemplateRegistry),
        ("SubmodelTemplateRegistry", c => c.SubmodelTemplateRegistry),
    ];

    public ValidateOptionsResult Validate(string? name, TemplateManagementConfig options)
    {
        if (options is null)
        {
            throw new InvalidDependencyException(nameof(options));
        }

        var errors = new List<string>();

        foreach (var (endpointName, accessor) in Endpoints)
        {
            var endpoint = accessor(options);
            if (endpoint.BaseUrl is null)
            {
                errors.Add($"{TemplateManagementConfig.Section}.{endpointName}.BaseUrl is required.");
            }
            else if (!endpoint.BaseUrl.IsAbsoluteUri)
            {
                errors.Add($"{TemplateManagementConfig.Section}.{endpointName}.BaseUrl must be an absolute URI, got: '{endpoint.BaseUrl}'.");
            }
        }

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }
}

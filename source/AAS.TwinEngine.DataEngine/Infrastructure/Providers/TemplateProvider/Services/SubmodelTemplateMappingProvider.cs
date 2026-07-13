using System.Text.RegularExpressions;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Observability;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.TemplateProvider.Services;

public class SubmodelTemplateMappingProvider(ILogger<SubmodelTemplateMappingProvider> logger, IOptions<TemplateManagementConfig> options) : ISubmodelTemplateMappingProvider
{
    private readonly IList<SubmodelTemplateMappings> _submodelTemplateMappings = options.Value.TemplateMappingRules.SubmodelTemplateMappings ?? throw new InvalidDependencyException(nameof(options.Value.TemplateMappingRules.SubmodelTemplateMappings), logger);
    private readonly TimeSpan _regexTimeout = TimeSpan.FromSeconds(2);

    public string? GetTemplateId(string submodelId)
    {
        using var activity = DataEngineDiagnostics.StartResolveSubmodelTemplateId(submodelId);

        var templateId = _submodelTemplateMappings
                         .Where(templatePattern => templatePattern.Pattern
                                                                  .Any(pattern => Regex.IsMatch(submodelId, pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled, _regexTimeout)))
                         .Select(templatePattern => templatePattern.TemplateId)
                         .FirstOrDefault();

        if (templateId != null)
        {
            activity?.SetTag(DataEngineDiagnostics.Attributes.TemplateId, templateId);
            return templateId;
        }

        logger.LogError("No matching template found for submodel: {SubmodelId}", submodelId);
        activity.RecordError("No matching template found");
        throw new ResourceNotFoundException();
    }
}

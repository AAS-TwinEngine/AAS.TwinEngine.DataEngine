using System.Text.RegularExpressions;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.TemplateProvider.Config;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.TemplateProvider.Services;

public class SubmodelTemplateMappingProvider(ILogger<SubmodelTemplateMappingProvider> logger, IOptions<TemplateMappingRules> options) : ISubmodelTemplateMappingProvider
{
    private readonly IList<SubmodelTemplateMappings> _submodelTemplateMappings = options.Value.SubmodelTemplateMappings ?? throw new ArgumentException("SubmodelTemplateMappings is missing in TemplateMappingSettings");
    private readonly TimeSpan _regexTimeout = TimeSpan.FromSeconds(2);

    public string? GetTemplateId(string submodelId)
    {
        var templateId = _submodelTemplateMappings
                         .Where(templatePattern => templatePattern.Pattern
                                                                  .Any(pattern => Regex.IsMatch(submodelId, pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled, _regexTimeout)))
                         .Select(templatePattern => templatePattern.TemplateId)
                         .FirstOrDefault();

        if (templateId != null)
        {
            return templateId;
        }

        logger.LogError("No matching template found for submodel: {SubmodelId}", submodelId);
        throw new ResourceNotFoundException();
    }
}

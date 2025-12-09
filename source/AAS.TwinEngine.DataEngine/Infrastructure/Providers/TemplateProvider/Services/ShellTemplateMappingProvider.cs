using System.Text.RegularExpressions;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.TemplateProvider.Config;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.TemplateProvider.Services;

public class ShellTemplateMappingProvider(ILogger<ShellTemplateMappingProvider> logger, IOptions<TemplateMappingRules> options) : IShellTemplateMappingProvider
{
    private readonly ILogger<ShellTemplateMappingProvider> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IList<ShellTemplateMappings> _shellTemplateMappings = options.Value.ShellTemplateMappings ?? throw new ArgumentException("ShellTemplateMappings are missing in TemplateMappingRules");
    private readonly IList<AasIdExtractionRules> _aasIdExtractionRules = options.Value.AasIdExtractionRules ?? throw new ArgumentException("AasIdExtractionRules are missing in TemplateMappingRules");
    private readonly TimeSpan _regexTimeout = TimeSpan.FromSeconds(2);

    public string? GetTemplateId(string aasIdentifier)
    {
        var productId = GetProductIdFromRule(aasIdentifier);

        var templateId = _shellTemplateMappings
            .FirstOrDefault(mapping => mapping.Pattern
                                              .Any(pattern => Regex.IsMatch(productId, pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled, _regexTimeout)))
            ?.TemplateId;

        if (templateId is not null)
        {
            return templateId;
        }

        _logger.LogError("No matching template found for shell: {aasIdentifier}", aasIdentifier);
        throw new ResourceNotFoundException();
    }

    public string GetProductIdFromRule(string aasIdentifier)
    {
        var productId = _aasIdExtractionRules
            .Select(rule => new
            {
                Rule = rule,
                Parts = aasIdentifier?.Split(rule.Separator)
            })
            .Where(x => x.Parts is { Length: >= 1 } && x.Rule.Index > 0 && x.Parts.Length >= x.Rule.Index)
            .Select(x => x.Parts![x.Rule.Index - 1])
            .FirstOrDefault();

        if (!string.IsNullOrEmpty(productId))
        {
            return productId;
        }

        _logger.LogError("ProductId could not be extracted from the provided aas Identifier.");
        throw new ResourceNotFoundException();
    }
}

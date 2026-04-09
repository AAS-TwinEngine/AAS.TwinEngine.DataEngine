using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

/// <summary>
/// Post-configuration step that applies the <see cref="TemplateManagementConfig.TemplateRepository"/>
/// shorthand: when <c>TemplateRepository</c> is provided, its BaseUrl and HeaderMappings are used
/// as defaults for AasTemplateRepository, SubmodelTemplateRepository, and
/// ConceptDescriptionTemplateRepository — unless they already have their own BaseUrl.
/// </summary>
public sealed class TemplateManagementConfigNormalizer : IPostConfigureOptions<TemplateManagementConfig>
{
    public void PostConfigure(string? name, TemplateManagementConfig options)
    {
        var fallback = options.TemplateRepository;
        if (fallback?.BaseUrl is null)
        {
            return;
        }

        ApplyFallback(options.AasTemplateRepository, fallback);
        ApplyFallback(options.SubmodelTemplateRepository, fallback);
        ApplyFallback(options.ConceptDescriptionTemplateRepository, fallback);
    }

    private static void ApplyFallback(ServiceEndpoint target, ServiceEndpoint fallback)
    {
        target.BaseUrl ??= fallback.BaseUrl;

        if (target.HeaderMappings.Count == 0 && fallback.HeaderMappings.Count > 0)
        {
            foreach (var mapping in fallback.HeaderMappings)
            {
                target.HeaderMappings.Add(mapping);
            }
        }
    }
}

using AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Config;

using System.ComponentModel.DataAnnotations;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Configuration.LegacyV1.ConfigV1;

#pragma warning disable S1133 
[Obsolete("V1 configuration is deprecated and will be removed in v2.0.0 version.")]
public class HeaderForwardingOptions
{
    public const string Section = "HeaderForwarding";

    [Required]
    public HeaderSanitizationOptions HeaderSanitization { get; set; } = new();

    [Required]
    public HeaderMappings HeaderMappings { get; set; } = new();
}

#pragma warning disable S1133 
[Obsolete("V1 configuration is deprecated and will be removed in v2.0.0 version.")]
public class HeaderMappings
{
    public IList<HeaderMappingRule> TemplateRepository { get; } = [];

    public IList<HeaderMappingRule> TemplateRegistry { get; } = [];

    public Dictionary<string, IList<HeaderMappingRule>> Plugins { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

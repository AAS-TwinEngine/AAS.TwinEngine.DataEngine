using System.ComponentModel.DataAnnotations;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Configuration.LegacyV1;

#pragma warning disable S1133 
[Obsolete("V1 configuration is deprecated and will be removed in next major release")]
public class HttpRetryPolicyOptions
{
    public const string Section = "HttpRetryPolicyOptions";

    public const string TemplateProvider = "TemplateProvider";

    public const string PluginDataProvider = "PluginDataProvider";

    public const string SubmodelDescriptorProvider = "SubmodelDescriptorProvider";

    [Required]
    public int MaxRetryAttempts { get; set; }

    [Required]
    public int DelayInSeconds { get; set; }
}

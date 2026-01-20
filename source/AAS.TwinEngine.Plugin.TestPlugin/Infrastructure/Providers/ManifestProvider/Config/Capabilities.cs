using System.ComponentModel.DataAnnotations;

namespace Aas.TwinEngine.Plugin.TestPlugin.Infrastructure.Providers.Config;

public class Capabilities
{
    public const string Section = "Capabilities";

    [Required]
    public bool HasShellDescriptor { get; set; }

    [Required]
    public bool HasAssetInformation { get; set; }
}

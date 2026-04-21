using System.ComponentModel.DataAnnotations;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Config;

public class HeaderMappingRule
{
    [Required]
    public string Source { get; set; } = null!;

    [Required]
    public string Target { get; set; } = null!;

    public bool Required { get; set; }
}

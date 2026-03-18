using System.ComponentModel.DataAnnotations;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Config;

public class HeaderForwardingOptions
{
    public const string Section = "HeaderForwarding";

    [Required]
    public HeaderSanitizationOptions HeaderSanitization { get; set; } = new();

    [Required]
    public HeaderMappings HeaderMappings { get; set; } = new();
}

public class HeaderSanitizationOptions
{
    [Range(1, int.MaxValue)]
    public int MaxHeaderSize { get; set; } = 8192;

    [Range(1, int.MaxValue)]
    public int MaxHeaderNameSize { get; set; } = 256;

    [Required]
    public AllowedCharactersOptions AllowedCharacters { get; set; } = new();

    public IList<string> BlockedPatterns { get; set; } = ["\\r|\\n", "\\x00", "<script"];
}

public class AllowedCharactersOptions
{
    [Required]
    public string HeaderNames { get; set; } = "^[a-zA-Z0-9\\-_]+$";

    [Required]
    public string HeaderValues { get; set; } = "^[\\x20-\\x7E]+$";
}

public class HeaderMappings
{
    public List<HeaderMappingRule> TemplateRepository { get; set; } = [];

    public List<HeaderMappingRule> TemplateRegistry { get; set; } = [];

    public Dictionary<string, List<HeaderMappingRule>> Plugins { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class HeaderMappingRule
{
    [Required]
    public string Source { get; set; } = null!;

    [Required]
    public string Target { get; set; } = null!;

    public bool Required { get; set; }
}

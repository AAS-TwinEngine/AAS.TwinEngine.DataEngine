using System.ComponentModel.DataAnnotations;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Config;

public class HeaderSanitizationOptions
{
    [Range(1, int.MaxValue)]
    public int MaxHeaderSize { get; set; } = 8192;

    [Range(1, int.MaxValue)]
    public int MaxHeaderNameSize { get; set; } = 256;

    [Required]
    public AllowedCharactersOptions AllowedCharacters { get; set; } = new();

    public IList<string> BlockedPatterns { get; init; } = ["\\r|\\n", "\\x00", "<script"];
}

public class AllowedCharactersOptions
{
    [Required]
    public string HeaderNames { get; set; } = "^[a-zA-Z0-9\\-_]+$";

    [Required]
    public string HeaderValues { get; set; } = "^[\\x20-\\x7E]+$";
}

public class HeaderMappingRule
{
    [Required]
    public string Source { get; set; } = null!;

    [Required]
    public string Target { get; set; } = null!;

    public bool Required { get; set; }
}

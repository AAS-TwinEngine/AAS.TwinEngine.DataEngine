using System.ComponentModel.DataAnnotations;

namespace AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

public class TemplateMappingRules
{
    public const string Section = "TemplateMappingRules";
    public IList<SubmodelTemplateMappings> SubmodelTemplateMappings { get; init; } = [];
    public IList<ShellTemplateMappings> ShellTemplateMappings { get; init; } = [];
    public IList<AasIdExtractionRule> AasIdExtractionRules { get; init; } = [];
}

public class SubmodelTemplateMappings
{
    public string TemplateId { get; set; } = string.Empty;
    public IList<string> Pattern { get; init; } = [];
}

public class ShellTemplateMappings
{
    public string TemplateId { get; set; } = string.Empty;
    public IList<string> Pattern { get; init; } = [];
}

public enum ExtractionStrategy
{
    Regex,
    Split
}

public class AasIdExtractionRule
{
    [Required]
    public ExtractionStrategy Strategy { get; set; }

    [Required]
    public string Pattern { get; set; } = string.Empty;

    [Required]
    public int Index { get; set; }

    public int? EndIndex { get; set; }

    public string? ValidationPattern { get; set; }
}

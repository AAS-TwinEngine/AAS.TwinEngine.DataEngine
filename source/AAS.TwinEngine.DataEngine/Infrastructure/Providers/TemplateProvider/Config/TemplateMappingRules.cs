namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.TemplateProvider.Config;

public class TemplateMappingRules
{
    public const string Section = "TemplateMappingRules";
    public IList<SubmodelTemplateMappings> SubmodelTemplateMappings { get; init; } = [];
    public IList<ShellTemplateMappings> ShellTemplateMappings { get; init; } = [];
    public IList<AasIdExtractionRules> AasIdExtractionRules { get; init; } = [];
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

public class AasIdExtractionRules
{
    public string Pattern { get; set; } = string.Empty;
    public int Index { get; set; }
    public string Separator { get; set; } = string.Empty;
}

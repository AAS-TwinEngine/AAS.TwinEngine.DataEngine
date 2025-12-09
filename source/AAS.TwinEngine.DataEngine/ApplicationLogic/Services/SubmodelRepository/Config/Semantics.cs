using System.ComponentModel.DataAnnotations;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.Config;

public class Semantics
{
    public const string Section = "Semantics";

    [Required]
    public string MultiLanguageSemanticPostfixSeparator { get; set; }

    [Required]
    public string SubmodelElementIndexContextPrefix { get; set; }

    [Required]
    public string InternalSemanticId { get; set; }
}

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.Config;

public class Semantics
{
    public const string Section = "Semantics";

    public string MultiLanguageSemanticPostfixSeparator { get; set; } = "_";

    public string SubmodelElementIndexContextPrefix { get; set; } = "_aastwinengineindex_";

    public string InternalSemanticId { get; set; } = "InternalSemanticId";
}

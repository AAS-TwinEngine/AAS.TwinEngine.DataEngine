namespace AAS.TwinEngine.DataEngine.Infrastructure.Configuration.LegacyV1;

#pragma warning disable S1133 
[Obsolete("V1 configuration is deprecated and will be removed in next major release")]
public class Semantics
{
    public const string Section = "Semantics";

    public string MultiLanguageSemanticPostfixSeparator { get; set; } = "_";

    public string SubmodelElementIndexContextPrefix { get; set; } = "_aastwinengineindex_";

    public string InternalSemanticId { get; set; } = "InternalSemanticId";
}

namespace AAS.TwinEngine.DataEngine.Infrastructure.Configuration.LegacyV1;

#pragma warning disable S1133 
[Obsolete("V1 configuration is deprecated and will be removed in next major release")]
public class MultiLanguagePropertySettings
{
    public const string Section = "MultiLanguageProperty";

    public IList<string>? DefaultLanguages { get; init; }
}

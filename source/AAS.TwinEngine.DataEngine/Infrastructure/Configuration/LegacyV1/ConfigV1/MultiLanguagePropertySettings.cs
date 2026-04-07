namespace AAS.TwinEngine.DataEngine.Infrastructure.Configuration.LegacyV1;

public class MultiLanguagePropertySettings
{
    public const string Section = "MultiLanguageProperty";

    public IList<string>? DefaultLanguages { get; init; }
}

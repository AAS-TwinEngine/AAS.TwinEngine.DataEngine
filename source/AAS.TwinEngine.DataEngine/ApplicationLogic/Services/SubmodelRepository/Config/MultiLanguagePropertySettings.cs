namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.Config;

public class MultiLanguagePropertySettings
{
    public const string Section = "MultiLanguageProperty";

    public IList<string>? DefaultLanguages { get; init; }
}

namespace AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Config;

public class HeaderSanitizationOptions
{
    public int MaxHeaderSize { get; set; } = 8192;

    public int MaxHeaderNameSize { get; set; } = 256;

    public AllowedCharactersOptions AllowedCharacters { get; set; } = new();

    public IList<string> BlockedPatterns { get; init; } = ["\\r|\\n", "\\x00", "<script"];
}

public class AllowedCharactersOptions
{
    public string HeaderNames { get; set; } = "^[a-zA-Z0-9\\-_]+$";

    public string HeaderValues { get; set; } = "^[\\x20-\\x7E]+$";
}

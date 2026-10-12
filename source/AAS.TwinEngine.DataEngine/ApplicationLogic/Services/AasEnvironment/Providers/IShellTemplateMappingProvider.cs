namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;

public interface IShellTemplateMappingProvider
{
    string? GetTemplateId(string aasIdentifier);

    /// <summary>
    /// Resolves the template and product IDs with one extraction pass. ProductId is null when no extraction rule matches.
    /// </summary>
    (string TemplateId, string? ProductId) GetTemplateMapping(string aasIdentifier);

    string GetProductIdFromRule(string aasIdentifier);
}

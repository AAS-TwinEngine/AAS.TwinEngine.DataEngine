namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;

public interface IShellTemplateMappingProvider
{
    string? GetTemplateId(string aasIdentifier);

    string GetProductIdFromRule(string aasIdentifier);
}

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;

public interface ISubmodelTemplateMappingProvider
{
    string? GetTemplateId(string submodelId);
}

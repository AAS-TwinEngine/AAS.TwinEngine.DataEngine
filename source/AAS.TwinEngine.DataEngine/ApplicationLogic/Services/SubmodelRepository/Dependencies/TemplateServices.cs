using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.Dependencies;

/// <summary>
/// Groups template-related submodel repository dependencies.
/// </summary>
public class TemplateServices(
    ISubmodelTemplateService submodelTemplateService,
    IAasRepositoryTemplateService aasRepositoryTemplateService,
    IOptions<TemplateManagementConfig> templateManagementConfig)
{
    /// <summary>
    /// Provides submodel templates.
    /// </summary>
    public ISubmodelTemplateService SubmodelTemplateService { get; } = submodelTemplateService;

    /// <summary>
    /// Provides AAS repository templates.
    /// </summary>
    public IAasRepositoryTemplateService AasRepositoryTemplateService { get; } = aasRepositoryTemplateService;

    /// <summary>
    /// Template management configuration.
    /// </summary>
    public TemplateManagementConfig Config { get; } = templateManagementConfig.Value;
}
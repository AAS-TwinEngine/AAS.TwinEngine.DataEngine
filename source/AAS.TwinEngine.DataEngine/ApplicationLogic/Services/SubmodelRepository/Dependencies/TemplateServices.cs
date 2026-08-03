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
    public ISubmodelTemplateService SubmodelTemplateService { get; } = submodelTemplateService;

    public IAasRepositoryTemplateService AasRepositoryTemplateService { get; } = aasRepositoryTemplateService;

    public TemplateManagementConfig Config { get; } = templateManagementConfig.Value;
}
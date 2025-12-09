using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Config;

using AasCore.Aas3_0;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;

public class AasRepositoryTemplateService(
    ITemplateProvider templateProvider,
    IShellTemplateMappingProvider shellTemplateMappingProvider,
    IOptions<AasEnvironmentConfig> aasEnvironment) : IAasRepositoryTemplateService
{
    private readonly ITemplateProvider _templateProvider = templateProvider ?? throw new ArgumentNullException(nameof(templateProvider));
    private readonly IShellTemplateMappingProvider _shellTemplateMappingProvider = shellTemplateMappingProvider ?? throw new ArgumentNullException(nameof(shellTemplateMappingProvider));
    private readonly Uri _customerDomainUrl = aasEnvironment.Value.CustomerDomainUrl ?? throw new ArgumentNullException(nameof(aasEnvironment.Value.CustomerDomainUrl));
    private const string SubmodelUrlSegment = "submodel";

    public async Task<IAssetAdministrationShell> GetShellTemplateAsync(string aasIdentifier, CancellationToken cancellationToken)
    {
        var shellTemplate = await GetTemplateAsync(aasIdentifier, _templateProvider.GetShellTemplateAsync, cancellationToken).ConfigureAwait(false);

        var productId = _shellTemplateMappingProvider.GetProductIdFromRule(aasIdentifier);

        foreach (var key in from submodel in shellTemplate?.Submodels
                            let key = submodel.Keys.FirstOrDefault()
                            where key != null
                            select key)
        {
            key.Value = _customerDomainUrl + string.Join('/', SubmodelUrlSegment, productId, key.Value);
        }

        return shellTemplate!;
    }

    public Task<IAssetInformation> GetAssetInformationTemplateAsync(string aasIdentifier, CancellationToken cancellationToken)
        => GetTemplateAsync(aasIdentifier, _templateProvider.GetAssetInformationTemplateAsync, cancellationToken);

    public async Task<List<IReference>> GetSubmodelRefByIdAsync(string aasIdentifier, CancellationToken cancellationToken)
    {
        try
        {
            var submodelRefList = await GetTemplateAsync(aasIdentifier, _templateProvider.GetSubmodelRefByIdAsync, cancellationToken).ConfigureAwait(false);
            var productId = _shellTemplateMappingProvider.GetProductIdFromRule(aasIdentifier);

            foreach (var key in submodelRefList.SelectMany(submodelRef => submodelRef.Keys!))
            {
                key.Value = _customerDomainUrl + string.Join('/', SubmodelUrlSegment, productId, key.Value);
            }

            return submodelRefList!;
        }
        catch (Exception ex)
        {
            throw new InternalDataProcessingException(ex);
        }
    }

    private async Task<T> GetTemplateAsync<T>(
        string aasIdentifier,
        Func<string, CancellationToken, Task<T>> templateFetchFunc,
        CancellationToken cancellationToken)
    {
        try
        {
            var templateId = _shellTemplateMappingProvider.GetTemplateId(aasIdentifier);

            return await templateFetchFunc(templateId!, cancellationToken).ConfigureAwait(false);
        }
        catch (ResourceNotFoundException ex)
        {
            throw new TemplateNotFoundException(ex);
        }
        catch (ResponseParsingException ex)
        {
            throw new InternalDataProcessingException(ex);
        }
        catch (RequestTimeoutException ex)
        {
            throw new RepositoryNotAvailableException(ex);
        }
    }
}

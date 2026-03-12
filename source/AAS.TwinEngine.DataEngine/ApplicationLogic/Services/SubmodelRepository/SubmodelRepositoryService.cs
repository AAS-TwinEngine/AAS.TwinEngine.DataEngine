using System.Diagnostics;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;

using AasCore.Aas3_0;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;

public class SubmodelRepositoryService(
    ISubmodelTemplateService submodelTemplateService,
    ISemanticIdHandler semanticIdHandler,
    IPluginDataHandler pluginDataHandler,
    IPluginManifestConflictHandler pluginManifestConflictHandler) : ISubmodelRepositoryService
{
    public async Task<ISubmodel> GetSubmodelAsync(string submodelId, CancellationToken cancellationToken)
    {
        return await ExecuteWithExceptionHandlingAsync(async () =>
        {
            var submodelTemplate = await submodelTemplateService.GetSubmodelTemplateAsync(submodelId, cancellationToken).ConfigureAwait(false);

            var submodelWithValues = await BuildSubmodelWithValuesAsync(submodelTemplate, submodelId, cancellationToken).ConfigureAwait(false);

            submodelWithValues.Id = submodelId;

            return submodelWithValues;
        }).ConfigureAwait(false);
    }

    public async Task<ISubmodelElement> GetSubmodelElementAsync(string submodelId, string idShortPath, CancellationToken cancellationToken)
    {
        return await ExecuteWithExceptionHandlingAsync(async () =>
        {
            var reducedSubmodelTemplate = await submodelTemplateService.GetSubmodelTemplateAsync(submodelId, idShortPath, cancellationToken).ConfigureAwait(false);

            var submodelWithValues = await BuildSubmodelWithValuesAsync(reducedSubmodelTemplate, submodelId, cancellationToken).ConfigureAwait(false);

            return semanticIdHandler.Extract(submodelWithValues, idShortPath);
        }).ConfigureAwait(false);
    }

    private async Task<ISubmodel> BuildSubmodelWithValuesAsync(ISubmodel template, string submodelId, CancellationToken cancellationToken)
    {
        var stopwatch1 = new Stopwatch();
        var stopwatch2 = new Stopwatch();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        stopwatch1.Start();
        var semanticIds = semanticIdHandler.Extract(template);
        stopwatch1.Stop();

        var pluginManifests = pluginManifestConflictHandler.Manifests;

        var values = await pluginDataHandler.TryGetValuesAsync(pluginManifests, semanticIds, submodelId, cancellationToken).ConfigureAwait(false);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        stopwatch2.Start();
        var result = semanticIdHandler.FillOutTemplate(template, values);
        stopwatch2.Stop();

        var time = stopwatch1.ElapsedMilliseconds + stopwatch2.ElapsedMilliseconds;
        result.Kind = ModellingKind.Instance;
        return result;
    }

    private static async Task<T> ExecuteWithExceptionHandlingAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action().ConfigureAwait(false);
        }
        catch (ResourceNotFoundException ex)
        {
            throw new SubmodelNotFoundException(ex);
        }
        catch (ResponseParsingException ex)
        {
            throw new InternalDataProcessingException(ex);
        }
        catch (RequestTimeoutException ex)
        {
            throw new PluginNotAvailableException(ex);
        }
        catch (MultiPluginConflictException ex)
        {
            throw new InternalDataProcessingException(ex);
        }
    }
}

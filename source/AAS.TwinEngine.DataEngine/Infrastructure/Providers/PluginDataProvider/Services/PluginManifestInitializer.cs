using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Providers;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Services;

public class PluginManifestInitializer(
    ILogger<PluginManifestInitializer> logger,
    IPluginManifestConflictHandler pluginManifestConflictHandler,
    IPluginManifestProvider pluginManifestProvider)
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting plugin registry initialization.");

        try
        {
            var manifests = (await pluginManifestProvider.GetAllPluginManifestsAsync(cancellationToken).ConfigureAwait(false)).ToList();

            await pluginManifestConflictHandler.ProcessManifests(manifests).ConfigureAwait(false);
        }
        catch (InternalDataProcessingException)
        {
            throw new MultiPluginConflictException();
        }
        catch (ResponseParsingException)
        {
            throw new MultiPluginConflictException();
        }
        catch (ResourceNotFoundException)
        {
            throw new MultiPluginConflictException();
        }

        logger.LogInformation("Plugin registry initialization completed successfully.");
    }
}

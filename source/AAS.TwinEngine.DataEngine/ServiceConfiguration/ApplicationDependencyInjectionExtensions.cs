using AAS.TwinEngine.DataEngine.Api.AasRegistry.Handler;
using AAS.TwinEngine.DataEngine.Api.AasRepository.Handler;
using AAS.TwinEngine.DataEngine.Api.Configuration;
using AAS.TwinEngine.DataEngine.Api.SubmodelRegistry.Handler;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Handler;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRegistry;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRegistry;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.AasRegistryProvider.Services;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Services;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.ServiceConfiguration;

public static class ApplicationDependencyInjectionExtensions
{
    public static void ConfigureApplication(this IServiceCollection services, IConfiguration configuration)
    {
        _ = services.AddExceptionHandler<GlobalExceptionHandler>();
        _ = services.AddProblemDetails();
        _ = services.Configure<ApiConfiguration>(configuration.GetSection("ApiConfiguration"));
        _ = services.AddSingleton(sp => sp.GetRequiredService<IOptions<ApiConfiguration>>().Value);
        _ = services.AddScoped<ISubmodelRepositoryHandler, SubmodelRepositoryHandler>();
        _ = services.AddScoped<IShellDescriptorHandler, ShellDescriptorHandler>();
        _ = services.AddScoped<IShellDescriptorService, ShellDescriptorService>();
        _ = services.AddScoped<IShellDescriptorDataHandler, ShellDescriptorDataHandler>();
        _ = services.AddScoped<ISubmodelDescriptorHandler, SubmodelDescriptorHandler>();
        _ = services.AddScoped<ISubmodelDescriptorService, SubmodelDescriptorService>();
        _ = services.AddScoped<IPluginDataHandler, PluginDataHandler>();
        _ = services.AddScoped<ISemanticIdHandler, SemanticIdHandler>();
        _ = services.AddScoped<ISubmodelRepositoryService, SubmodelRepositoryService>();
        _ = services.AddScoped<IConceptDescriptionService, ConceptDescriptionService>();
        _ = services.AddScoped<ISubmodelTemplateService, SubmodelTemplateService>();
        _ = services.AddScoped<IAasRepositoryHandler, AasRepositoryHandler>();
        _ = services.AddScoped<IAasRepositoryService, AasRepositoryService>();
        _ = services.AddScoped<ISerializationHandler, SerializationHandler>();
        _ = services.AddScoped<ISerializationService, SerializationService>();
        _ = services.AddScoped<IAasRepositoryTemplateService, AasRepositoryTemplateService>();
        _ = services.AddSingleton<IPluginManifestConflictHandler, PluginManifestConflictHandler>();
    }
}

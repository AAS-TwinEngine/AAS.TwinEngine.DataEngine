using Aas.TwinEngine.Plugin.TestPlugin.Api.Manifest.Handler;
using Aas.TwinEngine.Plugin.TestPlugin.Api.MetaData.Handler;
using Aas.TwinEngine.Plugin.TestPlugin.Api.Submodel.Handler;
using Aas.TwinEngine.Plugin.TestPlugin.Api.Submodel.Services;
using Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Exceptions;
using Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.Manifest;
using Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.MetaData;
using Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.Submodel;

using FluentValidation;
namespace Aas.TwinEngine.Plugin.TestPlugin.ServiceConfiguration;

public static class ApplicationDependencyInjectionExtensions
{
    public static void ConfigureApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatorsFromAssembly(typeof(ApplicationDependencyInjectionExtensions).Assembly);
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.AddScoped<IJsonSchemaParser, JsonSchemaParser>();
        services.AddScoped<ISemanticTreeHandler, SemanticTreeHandler>();
        services.AddScoped<IJsonSchemaValidator, JsonSchemaValidator>();
        services.AddScoped<ISubmodelService, SubmodelService>();
        services.AddScoped<ISubmodelHandler, SubmodelHandler>();

        services.AddScoped<IMetaDataService, MetaDataService>();
        services.AddScoped<IMetaDataHandler, MetaDataHandler>();

        services.AddScoped<IManifestService, ManifestService>();
        services.AddScoped<IManifestHandler, ManifestHandler>();
    }
}

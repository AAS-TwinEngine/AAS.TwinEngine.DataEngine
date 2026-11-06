using AAS.TwinEngine.ExportService.ApplicationLogic.Services.Crud;
using AAS.TwinEngine.ExportService.ApplicationLogic.Services.Export;

namespace AAS.TwinEngine.ExportService.ServiceConfiguration;

public static class ApplicationDependencyInjectionExtensions
{
    public static IServiceCollection ConfigureApplication(this IServiceCollection services)
    {
        _ = services.AddSingleton<ICrudDecisionMaker, CrudDecisionMaker>();

        _ = services.AddScoped<IPhaseExecutor, PhaseExecutor>();
        _ = services.AddScoped<IExportRunner, ExportRunner>();

        return services;
    }
}

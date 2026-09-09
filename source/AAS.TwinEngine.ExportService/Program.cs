using AAS.TwinEngine.ExportService.ServiceConfiguration;

using Serilog;

namespace AAS.TwinEngine.ExportService;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.ConfigureLogging(builder.Configuration);

        _ = builder.Services.AddHealthChecks();

        _ = builder.Services
            .ConfigureInfrastructure(builder.Configuration)
            .ConfigureApplication();

        var app = builder.Build();

        _ = app.MapHealthChecks("/healthz");

        try
        {
            await app.RunAsync().ConfigureAwait(false);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}

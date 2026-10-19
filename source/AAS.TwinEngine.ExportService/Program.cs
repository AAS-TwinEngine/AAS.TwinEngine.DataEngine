using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using AAS.TwinEngine.ExportService.ServiceConfiguration;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;

using Serilog;

namespace AAS.TwinEngine.ExportService;

[ExcludeFromCodeCoverage]
public static class Program
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

        _ = app.MapHealthChecks("/healthz", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var response = new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.ToDictionary(
                        entry => entry.Key,
                        entry => new
                        {
                            status = entry.Value.Status.ToString(),
                            description = entry.Value.Description
                        })
                };
                await JsonSerializer.SerializeAsync(
                    context.Response.Body,
                    response,
                    cancellationToken: context.RequestAborted).ConfigureAwait(false);
            }
        });

        try
        {
            await app.RunAsync().ConfigureAwait(false);
        }
        finally
        {
            await Log.CloseAndFlushAsync().ConfigureAwait(false);
        }
    }
}

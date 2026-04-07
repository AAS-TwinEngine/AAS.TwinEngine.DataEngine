using System.Diagnostics.CodeAnalysis;

using AAS.TwinEngine.DataEngine.Infrastructure.Logging;

using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace AAS.TwinEngine.DataEngine.ServiceConfiguration;

[ExcludeFromCodeCoverage]
internal static class LoggingConfigurationExtension
{
    public static void ConfigureLogging(this WebApplicationBuilder builder, IConfiguration configuration)
    {
        var otelSettings = configuration.GetSection($"{Config.GeneralConfig.Section}:{Config.OpenTelemetrySettings.Section}").Get<Config.OpenTelemetrySettings>() ?? new Config.OpenTelemetrySettings();

        var logLevelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);

        _ = builder.Services.AddSingleton(logLevelSwitch);

        _ = builder.Host.UseSerilog((context, loggerConfig) =>
        {
            // V2 config nests Serilog under "General:Serilog"; V1 keeps it at root "Serilog".
            // ReadFrom.Configuration looks for a child key named "Serilog" inside the section
            // you pass, so we pass the parent section ("General" or root) — not the Serilog
            // section itself.
            var generalSerilogSection = context.Configuration.GetSection("General:Serilog");
            var serilogParent = generalSerilogSection.Exists()
                ? context.Configuration.GetSection("General")
                : context.Configuration;

            _ = loggerConfig
                .ReadFrom.Configuration(serilogParent)
                .Enrich.FromLogContext()
                .Enrich.With<SanitizingEnricher>()
                .MinimumLevel.ControlledBy(logLevelSwitch);
        }, writeToProviders: true);

        _ = builder.Logging.ClearProviders();

        _ = builder.Logging.AddOpenTelemetry(options =>
        {
            options.IncludeScopes = true;
            options.IncludeFormattedMessage = true;
            options.ParseStateValues = true;
            _ = options.AddOtlpExporter(otlp => otlp.Endpoint = new Uri(otelSettings.OtlpEndpoint));
        });

        _ = builder.Services.AddOpenTelemetry()
               .ConfigureResource(resourceConfig => resourceConfig
                                      .AddService(
                                                  serviceName: otelSettings.ServiceName,
                                                  serviceVersion: otelSettings.ServiceVersion,
                                                  serviceInstanceId: Environment.MachineName))
               .WithTracing(tracerProvider =>
               {
                   _ = tracerProvider
                       .AddAspNetCoreInstrumentation()
                       .AddHttpClientInstrumentation()
                       .AddOtlpExporter(otlp => otlp.Endpoint = new Uri(otelSettings.OtlpEndpoint));
               })
               .WithMetrics(metricsProvider =>
               {
                   _ = metricsProvider
                       .AddAspNetCoreInstrumentation()
                       .AddHttpClientInstrumentation()
                       .AddOtlpExporter(otlp => otlp.Endpoint = new Uri(otelSettings.OtlpEndpoint));
               });
    }
}

using System.Diagnostics.CodeAnalysis;

using Aas.TwinEngine.Plugin.TestPlugin.ServiceConfiguration.Config;

using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Aas.TwinEngine.Plugin.TestPlugin.ServiceConfiguration;

[ExcludeFromCodeCoverage]
internal static class LoggingConfigurationExtension
{
    public static void ConfigureLogging(this WebApplicationBuilder builder, IConfiguration configuration)
    {
        var otelSettings = configuration.GetSection(OpenTelemetrySettings.Section).Get<OpenTelemetrySettings>() ?? new OpenTelemetrySettings();
        var logLevelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);

        _ = builder.Services.AddSingleton(logLevelSwitch);

        _ = builder.Host.UseSerilog((context, loggerConfig) =>
        {
            loggerConfig
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
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

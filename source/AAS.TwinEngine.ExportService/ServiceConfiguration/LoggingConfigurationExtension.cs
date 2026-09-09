using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using AAS.TwinEngine.ExportService.ApplicationLogic.Observability;
using AAS.TwinEngine.ExportService.Infrastructure.Logging;
using AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Serilog;

namespace AAS.TwinEngine.ExportService.ServiceConfiguration;

[ExcludeFromCodeCoverage]
internal static class LoggingConfigurationExtension
{
    private const string PeerServiceTag = "peer.service";

    public static void ConfigureLogging(this WebApplicationBuilder builder, IConfiguration configuration)
    {
        var observability = configuration
            .GetSection($"{ExportServiceConfig.Section}:{nameof(ExportServiceConfig.Observability)}")
            .Get<ObservabilityConfig>() ?? new ObservabilityConfig();

        _ = builder.Host.UseSerilog((context, loggerConfig) =>
        {
            _ = loggerConfig
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .Enrich.With<SanitizingEnricher>();
        }, writeToProviders: true);

        _ = builder.Logging.ClearProviders();
        _ = builder.Logging.AddOpenTelemetry(options =>
        {
            options.IncludeScopes = true;
            options.IncludeFormattedMessage = true;
            options.ParseStateValues = true;
            _ = options.AddOtlpExporter(otlp => otlp.Endpoint = new Uri(observability.OtlpEndpoint));
        });

        _ = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: observability.ServiceName,
                    serviceVersion: observability.ServiceVersion,
                    serviceInstanceId: Environment.MachineName))
            .WithTracing(tracing =>
            {
                _ = tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.EnrichWithHttpRequestMessage = static (activity, request) =>
                            SetPeerServiceTag(activity, request);
                    })
                    .AddSource(ExportServiceTracing.SourceName)
                    .AddOtlpExporter(otlp => otlp.Endpoint = new Uri(observability.OtlpEndpoint));
            })
            .WithMetrics(metrics =>
            {
                _ = metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(otlp => otlp.Endpoint = new Uri(observability.OtlpEndpoint));
            });
    }

    private static void SetPeerServiceTag(Activity activity, HttpRequestMessage request)
    {
        var host = request.RequestUri?.Host;
        if (string.IsNullOrWhiteSpace(host))
        {
            return;
        }

        _ = activity.SetTag(PeerServiceTag, host);
    }
}

using AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

using Microsoft.Extensions.Http.Resilience;

using Polly;

namespace AAS.TwinEngine.ExportService.Infrastructure.Http.Policies;

public static class ResilienceHandlerExtensions
{
    public static IHttpClientBuilder AddStandardResilience(this IHttpClientBuilder builder, ResilienceConfig config)
    {
        _ = builder.AddResilienceHandler("Retry", (pipelineBuilder, context) =>
        {
            _ = pipelineBuilder.AddRetry(new HttpRetryStrategyOptions
            {
                BackoffType = DelayBackoffType.Exponential,
                MaxRetryAttempts = config.MaxRetryAttempts,
                Delay = TimeSpan.FromSeconds(config.InitialDelaySeconds),
                UseJitter = true,
                OnRetry = args =>
                {
                    var loggerFactory = context.ServiceProvider.GetRequiredService<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger("ExportService.HttpResilience");
                    logger.LogWarning(
                        args.Outcome.Exception,
                        "HTTP retry attempt {AttemptNumber} after {DelaySeconds:F1}s.",
                        args.AttemptNumber, args.RetryDelay.TotalSeconds);
                    return default;
                }
            });
        });

        return builder;
    }
}

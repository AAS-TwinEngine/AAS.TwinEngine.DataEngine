using System.Diagnostics.CodeAnalysis;

using AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

using Microsoft.Extensions.Http.Resilience;

using Polly;
using Polly.Timeout;

namespace AAS.TwinEngine.ExportService.Infrastructure.Http.Policies;

public static class ResilienceHandlerExtensions
{
    public static IHttpClientBuilder AddStandardResilience(this IHttpClientBuilder builder, ResilienceConfig config)
    {
        _ = builder.AddResilienceHandler("Retry", (pipelineBuilder, context) =>
        {
            _ = pipelineBuilder.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = config.MaxRetryAttempts,
                DelayGenerator = args => ValueTask.FromResult<TimeSpan?>(
                    AddJitter(CalculateRetryDelay(args.AttemptNumber, config))),
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
            _ = pipelineBuilder.AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(config.TimeoutInSeconds)
            });
        });

        return builder;
    }

    internal static TimeSpan CalculateRetryDelay(int attemptNumber, ResilienceConfig config)
    {
        var delaySeconds = config.InitialDelaySeconds * Math.Pow(config.BackoffMultiplier, attemptNumber);
        return TimeSpan.FromSeconds(delaySeconds);
    }

    [SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "Retry jitter is not security-sensitive; Random.Shared provides the intended low-cost variation.")]
    private static TimeSpan AddJitter(TimeSpan delay)
    {
        var jitterFactor = 0.8 + (Random.Shared.NextDouble() * 0.4);
        return TimeSpan.FromTicks((long)(delay.Ticks * jitterFactor));
    }
}

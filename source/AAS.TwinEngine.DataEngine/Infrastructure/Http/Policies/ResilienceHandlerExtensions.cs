using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.Extensions.Http.Resilience;

using Polly;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Http.Policies;

public static class ResilienceHandlerExtensions
{
    public static IHttpClientBuilder AddStandardResilienceHandler(
        this IHttpClientBuilder httpClientBuilder,
        RetryConfig retryConfig)
    {
        _ = httpClientBuilder.AddResilienceHandler("Retry", (builder, context) =>
        {
            _ = builder.AddRetry(new HttpRetryStrategyOptions
            {
                BackoffType = DelayBackoffType.Exponential,
                MaxRetryAttempts = retryConfig.MaxRetryAttempts,
                Delay = TimeSpan.FromSeconds(retryConfig.DelayInSeconds),
                UseJitter = true,
                OnRetry = args =>
                {
                    var loggerFactory = context.ServiceProvider.GetRequiredService<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger("HttpResilience");
                    logger.LogWarning(args.Outcome.Exception, "Retry attempt {AttemptNumber} after {Delay}s", args.AttemptNumber, args.RetryDelay.TotalSeconds);
                    return default;
                }
            });
        });

        return httpClientBuilder;
    }
}

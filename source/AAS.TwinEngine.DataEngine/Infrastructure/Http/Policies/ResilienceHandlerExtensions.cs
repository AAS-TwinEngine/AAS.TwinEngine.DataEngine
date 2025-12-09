using AAS.TwinEngine.DataEngine.Infrastructure.Http.Config;

using Microsoft.Extensions.Options;

using Polly;
using Polly.Retry;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Http.Policies;

public static class ResilienceHandlerExtensions
{
    public static IHttpClientBuilder AddStandardResilienceHandler(
        this IHttpClientBuilder httpClientBuilder,
        string clientName)
    {
        _ = httpClientBuilder.AddResilienceHandler("Retry", (builder, context) =>
        {
            var optionsMonitor = context.ServiceProvider.GetRequiredService<IOptionsMonitor<HttpRetryPolicyOptions>>();
            var options = optionsMonitor.Get(clientName);

            _ = builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                BackoffType = DelayBackoffType.Exponential,
                MaxRetryAttempts = options.MaxRetryAttempts,
                Delay = TimeSpan.FromSeconds(options.DelayInSeconds),
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

using System.Net.Http.Headers;

using AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Headers;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Policies;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Http.Extensions;

public static class HttpClientRegistrationExtensions
{
    public static IServiceCollection AddHttpClientWithResilience(
        this IServiceCollection services,
        IConfiguration configuration,
        string clientName,
        string retryPolicySectionKey,
        Uri baseUrl)
    {
        _ = services.Configure<HttpRetryPolicyOptions>(configuration.GetSection($"{HttpRetryPolicyOptions.Section}:{retryPolicySectionKey}"));

        var httpClientBuilder = services.AddHttpClient(clientName, client =>
        {
            client.BaseAddress = baseUrl;
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        })
        .AddStandardResilienceHandler(retryPolicySectionKey);

        _ = httpClientBuilder.AddHttpMessageHandler(sp =>
                new HeaderForwardingHandler(
                    sp.GetRequiredService<IHttpContextAccessor>(),
                    sp.GetRequiredService<IRequestHeaderMapper>(),
                    clientName));

        return services;
    }
}

using System.Net.Http.Headers;
using System.Net;

using AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Authorization.Headers;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Policies;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Http.Extensions;

public static class HttpClientRegistrationExtensions
{
    private static readonly IReadOnlyCollection<string> AcceptEncodings = ["br", "gzip"];

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
            ConfigureCompression(client);
        })
        .AddStandardResilienceHandler(retryPolicySectionKey);

        httpClientBuilder.ConfigurePrimaryHttpMessageHandler(CreateHandler);

        _ = httpClientBuilder.AddHttpMessageHandler(sp =>
                new HeaderForwardingHandler(
                    sp.GetRequiredService<IHttpContextAccessor>(),
                    sp.GetRequiredService<IRequestHeaderMapper>(),
                    clientName));

        return services;
    }

    public static IServiceCollection AddHttpClientWithoutResilience(
        this IServiceCollection services,
        string clientName,
        Uri baseUrl,
        TimeSpan? timeout = null)
    {
        _ = services.AddHttpClient(clientName, client =>
        {
            client.BaseAddress = baseUrl;
            client.Timeout = timeout ?? TimeSpan.FromSeconds(5);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            ConfigureCompression(client);
        })
        .ConfigurePrimaryHttpMessageHandler(CreateHandler);

        return services;
    }

    private static void ConfigureCompression(HttpClient client)
    {
        foreach (var encoding in AcceptEncodings.Where(e => !string.IsNullOrWhiteSpace(e)))
        {
            client.DefaultRequestHeaders.AcceptEncoding.Add(
            new StringWithQualityHeaderValue(encoding));
        }
    }

    private static HttpMessageHandler CreateHandler()
    {
        return new HttpClientHandler
        {
            AutomaticDecompression = GetDecompressionMethods()
        };
    }

    private static DecompressionMethods GetDecompressionMethods()
    {
        return AcceptEncodings.Aggregate(DecompressionMethods.None, (current, encoding) => current | encoding switch
        {
            "gzip" => DecompressionMethods.GZip,
            "br" => DecompressionMethods.Brotli,
            _ => DecompressionMethods.None
        });
    }
}

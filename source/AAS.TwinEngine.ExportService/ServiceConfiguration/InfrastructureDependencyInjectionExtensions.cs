using System.Net.Http.Headers;

using AAS.TwinEngine.ExportService.ApplicationLogic.Services.Export;
using AAS.TwinEngine.ExportService.ApplicationLogic.Services.State;
using AAS.TwinEngine.ExportService.DomainModel;
using AAS.TwinEngine.ExportService.Infrastructure.Http.Authorization;
using AAS.TwinEngine.ExportService.Infrastructure.Http.Clients;
using AAS.TwinEngine.ExportService.Infrastructure.Http.Policies;
using AAS.TwinEngine.ExportService.Infrastructure.Scheduling;
using AAS.TwinEngine.ExportService.Infrastructure.State.DataAccess;
using AAS.TwinEngine.ExportService.Infrastructure.State.Migrations;
using AAS.TwinEngine.ExportService.ServiceConfiguration.Config;

namespace AAS.TwinEngine.ExportService.ServiceConfiguration;

public static class InfrastructureDependencyInjectionExtensions
{
    public static IServiceCollection ConfigureInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ── Options ──
        _ = services.AddOptions<ExportServiceConfig>()
            .Bind(configuration.GetSection(ExportServiceConfig.Section))
            .ValidateOnStart();

        var rootConfig = new ExportServiceConfig();
        configuration.GetSection(ExportServiceConfig.Section).Bind(rootConfig);

        // ── State store ──
        _ = services.AddSingleton<IDbConnectionFactory, PostgreSqlConnectionFactory>();
        _ = services.AddScoped<IStateStore, PostgreSqlStateStore>();
        _ = services.AddHostedService<StateSchemaInitializer>();

        // ── Auth: build endpoint-name → auth config map for the token provider ──
        var authByEndpoint = BuildAuthMap(rootConfig);
        _ = services.AddSingleton<IReadOnlyDictionary<string, AuthConfig>>(authByEndpoint);
        _ = services.AddSingleton<ITokenProvider, ConfiguredTokenProvider>();

        // ── HttpClients ──
        _ = services.AddHttpClient();

        // OAuth token acquisition client (no auth handler; short timeout)
        _ = services.AddHttpClient(HttpClientNames.OAuthToken, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        RegisterSourceHttpClient(services, HttpClientNames.SourceConceptDescriptions, rootConfig.Sources.ConceptDescriptions, rootConfig.Resilience);
        RegisterSourceHttpClient(services, HttpClientNames.SourceSubmodels, rootConfig.Sources.Submodels, rootConfig.Resilience);
        RegisterSourceHttpClient(services, HttpClientNames.SourceSubmodelDescriptors, rootConfig.Sources.SubmodelDescriptors, rootConfig.Resilience);
        RegisterSourceHttpClient(services, HttpClientNames.SourceShells, rootConfig.Sources.Shells, rootConfig.Resilience);
        RegisterSourceHttpClient(services, HttpClientNames.SourceShellDescriptors, rootConfig.Sources.ShellDescriptors, rootConfig.Resilience);

        RegisterTargetHttpClient(services, HttpClientNames.TargetConceptDescriptions, rootConfig.Targets.ConceptDescriptions, rootConfig.Resilience);
        RegisterTargetHttpClient(services, HttpClientNames.TargetSubmodels, rootConfig.Targets.Submodels, rootConfig.Resilience);
        RegisterTargetHttpClient(services, HttpClientNames.TargetSubmodelDescriptors, rootConfig.Targets.SubmodelDescriptors, rootConfig.Resilience);
        RegisterTargetHttpClient(services, HttpClientNames.TargetShells, rootConfig.Targets.Shells, rootConfig.Resilience);
        RegisterTargetHttpClient(services, HttpClientNames.TargetShellDescriptors, rootConfig.Targets.ShellDescriptors, rootConfig.Resilience);

        // ── Source & target façade services (Clean Architecture ports live in ApplicationLogic) ──
        _ = services.AddScoped<ISourceEntityReader, SourceEntityHttpClient>();
        _ = services.AddScoped<ITargetEntityWriter, TargetEntityHttpClient>();

        // ── Scheduling ──
        _ = services.AddSingleton<IExportRunLock, ExportRunLock>();
        _ = services.AddHostedService<ExportBackgroundService>();

        return services;
    }

    private static Dictionary<string, AuthConfig> BuildAuthMap(ExportServiceConfig config)
    {
        var map = new Dictionary<string, AuthConfig>(StringComparer.Ordinal);

        Add(map, HttpClientNames.SourceConceptDescriptions, config.Sources.ConceptDescriptions.Auth);
        Add(map, HttpClientNames.SourceSubmodels, config.Sources.Submodels.Auth);
        Add(map, HttpClientNames.SourceSubmodelDescriptors, config.Sources.SubmodelDescriptors.Auth);
        Add(map, HttpClientNames.SourceShells, config.Sources.Shells.Auth);
        Add(map, HttpClientNames.SourceShellDescriptors, config.Sources.ShellDescriptors.Auth);

        Add(map, HttpClientNames.TargetConceptDescriptions, config.Targets.ConceptDescriptions.Auth);
        Add(map, HttpClientNames.TargetSubmodels, config.Targets.Submodels.Auth);
        Add(map, HttpClientNames.TargetSubmodelDescriptors, config.Targets.SubmodelDescriptors.Auth);
        Add(map, HttpClientNames.TargetShells, config.Targets.Shells.Auth);
        Add(map, HttpClientNames.TargetShellDescriptors, config.Targets.ShellDescriptors.Auth);

        return map;

        static void Add(Dictionary<string, AuthConfig> map, string name, AuthConfig? auth)
        {
            if (auth is not null)
            {
                map[name] = auth;
            }
        }
    }

    private static void RegisterSourceHttpClient(IServiceCollection services, string clientName, EndpointConfig endpoint, ResilienceConfig resilience)
        => RegisterHttpClient(services, clientName, endpoint, resilience);

    private static void RegisterTargetHttpClient(IServiceCollection services, string clientName, EndpointConfig endpoint, ResilienceConfig resilience)
        => RegisterHttpClient(services, clientName, endpoint, resilience);

    private static void RegisterHttpClient(IServiceCollection services, string clientName, EndpointConfig endpoint, ResilienceConfig resilience)
    {
        var builder = services.AddHttpClient(clientName, client =>
        {
            if (!string.IsNullOrWhiteSpace(endpoint.BaseUrl))
            {
                client.BaseAddress = new Uri(endpoint.BaseUrl);
            }
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        _ = builder.AddHttpMessageHandler(sp => new BearerTokenHandler(
            sp.GetRequiredService<ITokenProvider>(),
            clientName));

        _ = builder.AddStandardResilience(resilience);
    }
}
